using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;
using Vigil.Core;
using Vigil.Core.Contracts;
using Vigil.Models;
using Vigil.Native;
using Vigil.Views;
using Application = System.Windows.Application;

namespace Vigil;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;
    private NotifyIcon? _trayIcon;
    private FlyoutWindow? _flyout;

    private SessionCoordinator? _coordinator;
    private PowerController? _powerController;
    private ThermalThrottler? _thermalThrottler;
    private WiFiWatchdog? _wifiWatchdog;
    private ProcessFreezer? _processFreezer;
    private RecoveryJournal? _recoveryJournal;
    private ProcessWatcher? _processWatcher;
    private PowerCfgRunner? _powerCfg;
    private VigilConfig _config = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load configuration
        LoadConfig();

        // Initialize core dependencies
        _powerCfg = new PowerCfgRunner();
        _recoveryJournal = new RecoveryJournal();
        _powerController = new PowerController(_powerCfg, _config);
        _thermalThrottler = new ThermalThrottler(_powerCfg, _config);
        _wifiWatchdog = new WiFiWatchdog(_config);
        _processFreezer = new ProcessFreezer(_config);

        _coordinator = new SessionCoordinator(
            _powerController,
            _thermalThrottler,
            _wifiWatchdog,
            _processFreezer,
            _recoveryJournal,
            _config
        );

        _processWatcher = new ProcessWatcher(_config);

        // Handle Command-Line Arguments
        if (e.Args.Length > 0)
        {
            if (Win32.AttachConsole(Win32.AttachParentProcess))
            {
                var stdOut = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                Console.SetOut(stdOut);
            }
            await HandleCliArgumentsAsync(e.Args);
            Environment.Exit(0);
            return;
        }

        // Single Instance Guard for UI Mode
        const string mutexName = "Vigil_SingleInstance_Mutex";
        _singleInstanceMutex = new Mutex(true, mutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            System.Windows.MessageBox.Show(
                "Vigil is already running in your System Tray notification area.",
                "Vigil",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            Shutdown();
            return;
        }

        // Check for previous crash journal
        await CheckCrashRecoveryAsync();

        // Initialize UI Flyout
        _flyout = new FlyoutWindow(_coordinator, _thermalThrottler, _wifiWatchdog, _processFreezer);

        // Setup System Tray Icon
        SetupSystemTray();

        // Show Flyout Window on launch
        ShowFlyout();

        // Setup Auto-Pilot
        _processWatcher.TargetProcessStarted += async (_, procName) =>
        {
            await _coordinator.StartSessionAsync(null, procName); // Infinite until process exits
            _flyout?.RefreshUi();
        };

        _processWatcher.TargetProcessExited += async (_, _) =>
        {
            await _coordinator.EndSessionAsync(SessionEndReason.TargetProcessExited);
            _flyout?.RefreshUi();
        };

        _processWatcher.Start();
    }

    private void SetupSystemTray()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(System.Drawing.Color.White), // Monochrome white icon
            Text = "Vigil - Clamshell & Cooling Guard",
            Visible = true
        };

        _trayIcon.Click += (s, args) =>
        {
            if (args is MouseEventArgs { Button: MouseButtons.Left })
            {
                ToggleFlyout();
            }
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open Vigil Dashboard", null, (_, _) => ShowFlyout());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Start 1 Hour Session", null, async (_, _) =>
        {
            await _coordinator!.StartSessionAsync(TimeSpan.FromHours(1));
            _flyout?.RefreshUi();
        });
        contextMenu.Items.Add("Start 2 Hours Session", null, async (_, _) =>
        {
            await _coordinator!.StartSessionAsync(TimeSpan.FromHours(2));
            _flyout?.RefreshUi();
        });
        contextMenu.Items.Add("End Active Session", null, async (_, _) =>
        {
            await _coordinator!.EndSessionAsync(SessionEndReason.UserRequested);
            _flyout?.RefreshUi();
        });
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Exit Vigil", null, (_, _) => ExitApp());

        _trayIcon.ContextMenuStrip = contextMenu;
    }

    private void ShowFlyout()
    {
        if (_flyout == null) return;
        if (_flyout.WindowState == WindowState.Minimized)
        {
            _flyout.WindowState = WindowState.Normal;
        }
        _flyout.PositionNearTray();
        _flyout.RefreshUi();
        _flyout.Show();
        _flyout.Activate();
    }

    private void ToggleFlyout()
    {
        if (_flyout == null) return;
        if (_flyout.IsVisible && _flyout.WindowState != WindowState.Minimized)
        {
            _flyout.Hide();
        }
        else
        {
            ShowFlyout();
        }
    }

    private async Task<bool> CheckCrashRecoveryAsync()
    {
        var activeJournal = await _recoveryJournal!.ReadActiveJournalAsync();
        if (activeJournal != null && activeJournal.Status == "Active")
        {
            // Perform clean rollback of previous crashed session
            if (activeJournal.OriginalSettings.AcLidAction.HasValue)
            {
                await _powerCfg!.SetLidActionAsync(activeJournal.OriginalSettings.AcLidAction.Value, false);
            }
            if (activeJournal.OriginalSettings.DcLidAction.HasValue)
            {
                await _powerCfg!.SetLidActionAsync(activeJournal.OriginalSettings.DcLidAction.Value, true);
            }
            if (activeJournal.OriginalSettings.AcMaxProcessorState.HasValue)
            {
                await _powerCfg!.SetMaxProcessorStateAsync(activeJournal.OriginalSettings.AcMaxProcessorState.Value, false);
            }
            if (activeJournal.OriginalSettings.DcMaxProcessorState.HasValue)
            {
                await _powerCfg!.SetMaxProcessorStateAsync(activeJournal.OriginalSettings.DcMaxProcessorState.Value, true);
            }

            // Unfreeze any frozen processes
            ProcessFreezer.ResumePids(activeJournal.FrozenProcessIds);

            await _recoveryJournal.ClearJournalAsync();
            await _recoveryJournal.CancelBackstopWatchdogAsync();
            return true;
        }

        return false;
    }

    private async Task HandleCliArgumentsAsync(string[] args)
    {
        var cmd = args[0].ToLowerInvariant();

        if (cmd == "--help" || cmd == "-h" || cmd == "/?")
        {
            Console.WriteLine("Vigil - Clamshell & Cooling Guard for Long-Running AI Workloads");
            Console.WriteLine("Usage: Vigil.exe [options]");
            Console.WriteLine("  --session start [seconds]   Start a clamshell sleep-prevention & cooling session");
            Console.WriteLine("  --session extend [seconds]  Extend active session");
            Console.WriteLine("  --session end               End active session and restore power settings");
            Console.WriteLine("  --session status [--json]   Get current session and sensor status");
            Console.WriteLine("  --recover                   Recover power settings and thaw processes from crash journal");
            Console.WriteLine("  --help, -h                  Show this help message");
            return;
        }

        if (cmd == "--recover")
        {
            var recovered = await CheckCrashRecoveryAsync();
            if (recovered)
            {
                Console.WriteLine("{\"status\":\"recovered\",\"recovered_from_journal\":true,\"message\":\"System power settings and frozen processes restored successfully from crash journal.\"}");
            }
            else
            {
                Console.WriteLine("{\"status\":\"clean\",\"recovered_from_journal\":false,\"message\":\"No orphaned crash journal detected. System is clean.\"}");
            }
            return;
        }

        if (cmd == "--session" && args.Length > 1)
        {
            var subCmd = args[1].ToLowerInvariant();
            switch (subCmd)
            {
                case "start":
                    var sec = args.Length > 2 && int.TryParse(args[2], out var s) ? s : 7200;
                    await _coordinator!.StartSessionAsync(TimeSpan.FromSeconds(sec));
                    Console.WriteLine($"{{\"status\":\"started\",\"duration_seconds\":{sec}}}");
                    break;

                case "extend":
                    var extSec = args.Length > 2 && int.TryParse(args[2], out var es) ? es : 1800;
                    await _coordinator!.ExtendSessionAsync(TimeSpan.FromSeconds(extSec));
                    Console.WriteLine($"{{\"status\":\"extended\",\"added_seconds\":{extSec}}}");
                    break;

                case "end":
                    await _coordinator!.EndSessionAsync(SessionEndReason.UserRequested);
                    Console.WriteLine("{\"status\":\"ended\"}");
                    break;

                case "status":
                    var isJson = args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase));
                    if (isJson)
                    {
                        var statusObj = new
                        {
                            status = _coordinator!.CurrentStatus.ToString(),
                            remaining_seconds = (int)_coordinator.RemainingTime.TotalSeconds,
                            target_process = _coordinator.ActiveTargetProcess,
                            cpu_temp_celsius = _thermalThrottler!.CurrentCpuTempCelsius,
                            cpu_max_state = _thermalThrottler.CurrentMaxProcessorState,
                            wifi_ssid = _wifiWatchdog!.CurrentConnectedSsid
                        };
                        Console.WriteLine(JsonSerializer.Serialize(statusObj));
                    }
                    else
                    {
                        Console.WriteLine($"Vigil Session: {_coordinator!.CurrentStatus}, Remaining: {_coordinator.RemainingTime}");
                    }
                    break;
            }
        }
    }

    private void LoadConfig()
    {
        try
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Vigil",
                "config.json"
            );

            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                _config = JsonSerializer.Deserialize<VigilConfig>(json) ?? new VigilConfig();
            }
        }
        catch
        {
            _config = new VigilConfig();
        }
    }

    private static Icon CreateTrayIcon(System.Drawing.Color color)
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var icoPath = Path.Combine(baseDir, "icon.ico");
            if (File.Exists(icoPath))
            {
                return new Icon(icoPath, 32, 32);
            }
        }
        catch
        {
            // Fallback to procedural icon
        }

        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var pen = new System.Drawing.Pen(color, 2.5f);
        pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
        pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

        // Draw Minimalist 'V' with Tilted Orbit
        g.DrawLine(pen, 6, 8, 16, 25);
        g.DrawLine(pen, 16, 25, 26, 8);
        using var starBrush = new SolidBrush(System.Drawing.Color.White);
        g.FillEllipse(starBrush, 14, 12, 4, 4);

        var hIcon = bmp.GetHicon();
        return (Icon)Icon.FromHandle(hIcon).Clone();
    }

    private async void ExitApp()
    {
        if (_coordinator != null)
        {
            await _coordinator.EndSessionAsync(SessionEndReason.AppExiting);
        }

        _trayIcon?.Dispose();
        _singleInstanceMutex?.Dispose();
        Shutdown();
    }
}
