using System.Diagnostics;
using System.Runtime.InteropServices;
using Vigil.Core;
using Vigil.Core.Contracts;
using Vigil.Models;

namespace Vigil.Native;

public class GlobalEscapeWatcher : IDisposable
{
    private readonly ISessionCoordinator _coordinator;
    private readonly IPowerController _power;
    private readonly Action? _onEmergencyCancel;
    private readonly Win32.LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private volatile bool _isDisplayOff;
    private bool _disposed;

    public bool IsDisplayOff
    {
        get => _isDisplayOff;
        set => _isDisplayOff = value;
    }

    public GlobalEscapeWatcher(
        ISessionCoordinator coordinator,
        IPowerController power,
        Action? onEmergencyCancel = null)
    {
        _coordinator = coordinator;
        _power = power;
        _onEmergencyCancel = onEmergencyCancel;

        _proc = HookCallback;
        _hookId = SetHook(_proc);
    }

    private IntPtr SetHook(Win32.LowLevelKeyboardProc proc)
    {
        try
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            var moduleHandle = curModule != null ? Win32.GetModuleHandle(curModule.ModuleName) : IntPtr.Zero;
            return Win32.SetWindowsHookEx(Win32.WhKeyboardLl, proc, moduleHandle, 0);
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)Win32.WmKeyDown || wParam == (IntPtr)Win32.WmSysKeyDown))
        {
            var vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == Win32.VkEscape)
            {
                var isSessionActive = _coordinator.CurrentStatus == SessionStatus.Active;
                var isBlackscreen = _power.IsScreenBlanked || _isDisplayOff;

                if (isSessionActive && isBlackscreen)
                {
                    HandleEscapeCancel();
                    // Consume the Escape key so background applications don't react
                    return (IntPtr)1;
                }
            }
        }

        return Win32.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void HandleEscapeCancel()
    {
        // 1. Instantly wake physical screen backlight and signal display pipeline
        _power.WakeScreen();
        _isDisplayOff = false;

        // 2. Terminate active guard session asynchronously
        _ = Task.Run(async () =>
        {
            await _coordinator.EndSessionAsync(SessionEndReason.UserRequested);
        });

        // 3. Invoke UI callback (show flyout near tray, refresh UI)
        _onEmergencyCancel?.Invoke();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_hookId != IntPtr.Zero)
            {
                Win32.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
