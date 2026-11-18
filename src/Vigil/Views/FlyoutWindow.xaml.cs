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

        // Startup auto-launch checkbox initial state
        ChkAutoStart.IsChecked = StartupManager.IsRunOnStartupEnabled();
        ChkAutoStart.Checked += (_, _) => StartupManager.SetRunOnStartup(true);
        ChkAutoStart.Unchecked += (_, _) => StartupManager.SetRunOnStartup(false);

        // Wi-Fi watchdog checkbox
        ChkAutoWifi.IsChecked = _config.WiFi.WatchdogEnabled;
        ChkAutoWifi.Checked += async (_, _) =>
        {
            _config.WiFi.WatchdogEnabled = true;
            if (!_isInitializing)
            {
                if (_coordinator.CurrentStatus == SessionStatus.Active)
                {
                    await _wifi.ApplyAsync();
                }
                RefreshUi();
            }
        };
        ChkAutoWifi.Unchecked += async (_, _) =>
        {
            _config.WiFi.WatchdogEnabled = false;
            if (!_isInitializing)
            {
                await _wifi.RevertAsync();
                RefreshUi();
            }
        };

        _isInitializing = false;

        PositionNearTray();
        RefreshUi();
    }

    public void PositionNearTray(bool force = false)
    {
        if (_hasUserMoved && !force) return;

        var screenWidth = SystemParameters.WorkArea.Width;
        var screenHeight = SystemParameters.WorkArea.Height;

        Left = screenWidth - Width - 16;
        Top = screenHeight - Height - 8;
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
                }
                else
                {
                    TxtAutoPilotBadge.Text = "STANDBY";
                    TxtAutoPilotBadge.Foreground = (SolidColorBrush)FindResource("BrushTextMuted");
                    TxtTargetStatus.Text = "System Standby";

                    BtnEndSession.Content = "Close (ESC)";
                    BtnEndSession.Style = (Style)FindResource("BentoDangerBtn");
                    BtnEndSession.ToolTip = "Close window to system tray (ESC)";
                }
            }
            finally
            {
                _isSyncingMasterSwitch = false;
            }

            // 3. Thermal State
            var temp = _thermal.CurrentCpuTempCelsius;
            TxtTempDigits.Text = temp > 0 ? $"{temp:F0}°C" : "--°C";

            if (_thermal.IsEmergencyThrottled)
            {
                TxtTempStatus.Text = "Thermal Capped";
                TxtTempStatus.Foreground = (SolidColorBrush)FindResource("BrushTextWhite");
            }
            else
            {
                TxtTempStatus.Text = _thermal.CurrentCpuLoadPercent > 0
                    ? $"{_thermal.CurrentCpuLoadPercent}% Load • 92% Cap"
                    : "Normal • 92% Cap";
                TxtTempStatus.Foreground = (SolidColorBrush)FindResource("BrushTextSilver");
            }

            // 4. Wi-Fi Watchdog State
            if (ChkAutoWifi.IsChecked != true)
            {
                TxtWifiStatus.Text = "Disabled";
                TxtWifiStatus.Foreground = (SolidColorBrush)FindResource("BrushTextDim");
            }
            else if (string.IsNullOrEmpty(_wifi.CurrentConnectedSsid))
            {
                TxtWifiStatus.Text = "Disconnected";
                TxtWifiStatus.Foreground = (SolidColorBrush)FindResource("BrushTextDim");
            }
            else
            {
                var ssid = _wifi.CurrentConnectedSsid;
                var displaySsid = ssid.Length > 12 ? ssid[..10] + "…" : ssid;
                TxtWifiStatus.Text = $"{_wifi.PingLatencyMs}ms ({displaySsid})";
                TxtWifiStatus.Foreground = (SolidColorBrush)FindResource("BrushTextSilver");
            }

            // 5. Battery & Power Status
            try
            {
                var power = System.Windows.Forms.SystemInformation.PowerStatus;
                var pct = (int)(power.BatteryLifePercent * 100);
                var isAc = power.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;

                if (isAc)
                {
                    TxtBatStatus.Text = $"AC Power ({pct}%)";
                }
                else
                {
                    if (power.BatteryLifeRemaining > 0)
                    {
                        var hrs = power.BatteryLifeRemaining / 3600;
                        var mins = (power.BatteryLifeRemaining % 3600) / 60;
                        TxtBatStatus.Text = $"Battery {pct}% (~{hrs}h {mins}m)";
                    }
                    else
                    {
                        TxtBatStatus.Text = $"Battery ({pct}%)";
                    }
                }
            }
            catch
            {
                TxtBatStatus.Text = "AC Power (100%)";
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
