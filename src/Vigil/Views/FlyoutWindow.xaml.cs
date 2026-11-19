using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Vigil.Core;
using Vigil.Core.Contracts;
using Vigil.Models;
using Vigil.Native;

namespace Vigil.Views;

public partial class FlyoutWindow : Window
{
    private readonly ISessionCoordinator _coordinator;
    private readonly IThermalThrottler _thermal;
    private readonly IWiFiWatchdog _wifi;
    private readonly IProcessFreezer _freezer;
    private readonly VigilConfig _config;
    private GlobalEscapeWatcher? _escapeWatcher;
    private IntPtr _displayNotifyHandle = IntPtr.Zero;
    private IntPtr _monitorNotifyHandle = IntPtr.Zero;
    private bool _isInitializing = true;
    private bool _isSyncingMasterSwitch = false;
    private bool _hasUserMoved = false;

    public FlyoutWindow(
        ISessionCoordinator coordinator,
        IThermalThrottler thermal,
        IWiFiWatchdog wifi,
        IProcessFreezer freezer,
        VigilConfig config)
    {
        InitializeComponent();

        _coordinator = coordinator;
        _thermal = thermal;
        _wifi = wifi;
        _freezer = freezer;
        _config = config;

        _coordinator.StateChanged += Coordinator_StateChanged;
        _thermal.TemperatureUpdated += Thermal_TemperatureUpdated;
        _wifi.StatusChanged += Wifi_StatusChanged;
        PreviewKeyDown += FlyoutWindow_PreviewKeyDown;

        // Master Vigil protection toggle switch
        ChkMasterVigil.Checked += async (_, _) =>
        {
            if (_isSyncingMasterSwitch || _isInitializing) return;
            if (_coordinator.CurrentStatus != SessionStatus.Active)
            {
                await _coordinator.StartSessionAsync(null); // Infinite manual session
                RefreshUi();
            }
        };

        ChkMasterVigil.Unchecked += async (_, _) =>
        {
            if (_isSyncingMasterSwitch || _isInitializing) return;
            if (_coordinator.CurrentStatus == SessionStatus.Active)
            {
                await _coordinator.EndSessionAsync(SessionEndReason.UserRequested);
                RefreshUi();
            }
        };

        _isInitializing = false;

        PositionNearTray();
        RefreshUi();
    }

    public void SetEscapeWatcher(GlobalEscapeWatcher watcher)
    {
        _escapeWatcher = watcher;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var source = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);

        var guidDisplay = Win32.GuidConsoleDisplayState;
        _displayNotifyHandle = Win32.RegisterPowerSettingNotification(hwnd, ref guidDisplay, 0);

        var guidMonitor = Win32.GuidMonitorPowerOn;
        _monitorNotifyHandle = Win32.RegisterPowerSettingNotification(hwnd, ref guidMonitor, 0);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32.WmPowerBroadcast && wParam == (IntPtr)Win32.PbtPowerSettingChange)
        {
            try
            {
                var setting = System.Runtime.InteropServices.Marshal.PtrToStructure<Win32.PowerBroadcastSetting>(lParam);
                if (setting.PowerSetting == Win32.GuidConsoleDisplayState || setting.PowerSetting == Win32.GuidMonitorPowerOn)
                {
                    var isOff = setting.Data == 0;
                    if (_escapeWatcher != null)
                    {
                        _escapeWatcher.IsDisplayOff = isOff;
                    }
                }
            }
            catch { }
        }
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_displayNotifyHandle != IntPtr.Zero)
        {
            Win32.UnregisterPowerSettingNotification(_displayNotifyHandle);
            _displayNotifyHandle = IntPtr.Zero;
        }
        if (_monitorNotifyHandle != IntPtr.Zero)
        {
            Win32.UnregisterPowerSettingNotification(_monitorNotifyHandle);
            _monitorNotifyHandle = IntPtr.Zero;
        }
        base.OnClosed(e);
    }

    public void PositionNearTray(bool force = false)
    {
        if (_hasUserMoved && !force) return;

        var screenWidth = SystemParameters.WorkArea.Width;
        var screenHeight = SystemParameters.WorkArea.Height;

        Left = screenWidth - Width - 12;
        Top = screenHeight - Height - 6;
    }

    public void RefreshUi()
    {
        Dispatcher.Invoke(() =>
        {
            var isActive = _coordinator.CurrentStatus == SessionStatus.Active;

            // 1. Session Countdown Timer
            if (_coordinator.IsInfiniteSession)
            {
                TxtTimerDigits.Text = "∞";
            }
            else if (isActive)
            {
                var rem = _coordinator.RemainingTime;
                TxtTimerDigits.Text = $"{rem.Hours:D2}:{rem.Minutes:D2}:{rem.Seconds:D2}";
            }
            else
            {
                TxtTimerDigits.Text = "--:--:--";
            }

            // 2. Master Vigil Guard & Target Status
            _isSyncingMasterSwitch = true;
            try
            {
                ChkMasterVigil.IsChecked = isActive;
                TxtMasterSwitchLabel.Text = isActive ? "ON" : "OFF";
                TxtMasterSwitchLabel.Foreground = isActive
                    ? (SolidColorBrush)FindResource("BrushTextWhite")
                    : (SolidColorBrush)FindResource("BrushTextDim");

                if (isActive)
                {
                    if (!string.IsNullOrEmpty(_coordinator.ActiveTargetProcess))
                    {
                        TxtAutoPilotBadge.Text = "ACTIVE";
                        TxtAutoPilotBadge.Foreground = (SolidColorBrush)FindResource("BrushTextWhite");
                        TxtTargetStatus.Text = $"Watching {_coordinator.ActiveTargetProcess}";
                    }
                    else
                    {
                        TxtAutoPilotBadge.Text = "GUARDING";
                        TxtAutoPilotBadge.Foreground = (SolidColorBrush)FindResource("BrushTextWhite");
                        TxtTargetStatus.Text = "Clamshell Guard Active";
                    }

                    BtnEndSession.Content = "Stop (ESC)";
                    BtnEndSession.Style = (Style)FindResource("BentoStopActiveBtn");
                    BtnEndSession.ToolTip = "Stop protection and restore normal sleep settings (ESC)";
                    BtnToggleCustom.Content = "+ Custom";
                    BtnStartCustom.Content = "+ Add";
                }
                else
                {
                    TxtAutoPilotBadge.Text = "STANDBY";
                    TxtAutoPilotBadge.Foreground = (SolidColorBrush)FindResource("BrushTextMuted");
                    TxtTargetStatus.Text = "System Standby";

                    BtnEndSession.Content = "Close (ESC)";
                    BtnEndSession.Style = (Style)FindResource("BentoDangerBtn");
                    BtnEndSession.ToolTip = "Close window to system tray (ESC)";
                    BtnToggleCustom.Content = "Custom";
                    BtnStartCustom.Content = "Start";
                }
            }
            finally
            {
                _isSyncingMasterSwitch = false;
            }

            // 3. Thermal State (Micro)
            var temp = _thermal.CurrentCpuTempCelsius;
            TxtTempDigits.Text = temp > 0 ? $"{temp:F0}°C" : "--°C";

            if (_thermal.IsEmergencyThrottled)
            {
                TxtTempStatus.Text = "Throttled";
                TxtTempStatus.Foreground = (SolidColorBrush)FindResource("BrushTextWhite");
            }
            else
            {
                TxtTempStatus.Text = $"{_thermal.CurrentMaxProcessorState}% Cap";
                TxtTempStatus.Foreground = (SolidColorBrush)FindResource("BrushTextDim");
            }

            // 4. Wi-Fi Watchdog State (Micro)
            if (!_config.WiFi.WatchdogEnabled)
            {
                TxtWifiStatus.Text = "Wi-Fi Off";
                TxtWifiStatus.Foreground = (SolidColorBrush)FindResource("BrushTextDim");
            }
            else if (string.IsNullOrEmpty(_wifi.CurrentConnectedSsid))
            {
                TxtWifiStatus.Text = "Offline";
                TxtWifiStatus.Foreground = (SolidColorBrush)FindResource("BrushTextDim");
            }
            else
            {
                TxtWifiStatus.Text = $"{_wifi.PingLatencyMs}ms";
                TxtWifiStatus.Foreground = (SolidColorBrush)FindResource("BrushTextSilver");
            }

            // 5. Battery & Power Status (Micro)
            try
            {
                var power = System.Windows.Forms.SystemInformation.PowerStatus;
                var pct = (int)(power.BatteryLifePercent * 100);
                var isAc = power.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;

                if (isAc)
                {
                    TxtBatStatus.Text = $"AC ({pct}%)";
                }
                else
                {
                    TxtBatStatus.Text = $"Bat ({pct}%)";
                }
            }
            catch
            {
                TxtBatStatus.Text = "AC (100%)";
            }
        });
    }

    private void Coordinator_StateChanged(object? sender, SessionStateChangedEventArgs e) => RefreshUi();
    private void Thermal_TemperatureUpdated(object? sender, float e) => RefreshUi();
    private void Wifi_StatusChanged(object? sender, WiFiStatusChangedEventArgs e) => RefreshUi();

    private void BtnCloseFlyout_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWin = new SettingsWindow(_config);
        settingsWin.ShowDialog();
        RefreshUi();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            _hasUserMoved = true;
            DragMove();
        }
    }

    private async void BtnAdd30_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator.CurrentStatus == SessionStatus.Active)
        {
            await _coordinator.ExtendSessionAsync(TimeSpan.FromMinutes(30));
        }
        else
        {
            await _coordinator.StartSessionAsync(TimeSpan.FromMinutes(30));
        }
        RefreshUi();
    }

    private async void BtnEndSession_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator.CurrentStatus == SessionStatus.Active)
        {
            await _coordinator.EndSessionAsync(SessionEndReason.UserRequested);
            RefreshUi();
        }
        else
        {
            Hide();
        }
    }

    private void BtnToggleCustom_Click(object sender, RoutedEventArgs e) => OpenCustomPanel();
    private void TxtTimerDigits_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => OpenCustomPanel();

    private void OpenCustomPanel()
    {
        GridPresets.Visibility = Visibility.Collapsed;
        PanelCustomTime.Visibility = Visibility.Visible;
        TxtCustomHours.Focus();
        TxtCustomHours.SelectAll();
    }

    private void BtnCloseCustom_Click(object sender, RoutedEventArgs e) => CloseCustomPanel();

    private void CloseCustomPanel()
    {
        PanelCustomTime.Visibility = Visibility.Collapsed;
        GridPresets.Visibility = Visibility.Visible;
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        var regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }

    private void CustomInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnStartCustom_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CloseCustomPanel();
            e.Handled = true;
        }
    }

    private async void BtnStartCustom_Click(object sender, RoutedEventArgs e)
    {
        int.TryParse(TxtCustomHours.Text, out var hours);
        int.TryParse(TxtCustomMinutes.Text, out var mins);

        if (hours <= 0 && mins <= 0)
        {
            mins = 30;
        }

        var duration = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(mins);
        if (_coordinator.CurrentStatus == SessionStatus.Active)
        {
            await _coordinator.ExtendSessionAsync(duration);
        }
        else
        {
            await _coordinator.StartSessionAsync(duration);
        }

        CloseCustomPanel();
        RefreshUi();
    }

    private async void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string tagStr } && int.TryParse(tagStr, out var sec))
        {
            if (sec < 0)
            {
                await _coordinator.StartSessionAsync(null); // Infinite
            }
            else
            {
                await _coordinator.StartSessionAsync(TimeSpan.FromSeconds(sec));
            }
            RefreshUi();
        }
    }

    private async void FlyoutWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (PanelCustomTime.Visibility == Visibility.Visible)
            {
                CloseCustomPanel();
                e.Handled = true;
                return;
            }

            if (_coordinator.CurrentStatus == SessionStatus.Active)
            {
                await _coordinator.EndSessionAsync(SessionEndReason.UserRequested);
                RefreshUi();
            }
            else
            {
                Hide();
            }
            e.Handled = true;
        }
    }
}
