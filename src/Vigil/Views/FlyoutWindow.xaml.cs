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

    private bool _hasUserMoved = false;

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
            // 1. Session Timer
            if (_coordinator.IsInfiniteSession)
            {
                TxtTimerDigits.Text = "∞";
            }
            else if (_coordinator.CurrentStatus == SessionStatus.Active)
            {
                var rem = _coordinator.RemainingTime;
                TxtTimerDigits.Text = $"{rem.Hours:D2}:{rem.Minutes:D2}:{rem.Seconds:D2}";
            }
            else
            {
                TxtTimerDigits.Text = "--:--:--";
            }

            // 2. Master Vigil Guard & AutoPilot Status
            _isSyncingMasterSwitch = true;
            try
            {
                var isActive = _coordinator.CurrentStatus == SessionStatus.Active;
                ChkMasterVigil.IsChecked = isActive;
                TxtMasterSwitchLabel.Text = isActive ? "ON" : "OFF";
                TxtMasterSwitchLabel.Foreground = isActive
                    ? (SolidColorBrush)FindResource("BrushTextWhite")
                    : (SolidColorBrush)FindResource("BrushTextDim");

                if (isActive)
                {
                    DotStatus.Fill = (SolidColorBrush)FindResource("BrushTextWhite");
                    if (!string.IsNullOrEmpty(_coordinator.ActiveTargetProcess))
                    {
                        TxtTargetProcess.Text = _coordinator.ActiveTargetProcess;
                        TxtTargetDesc.Text = " • AI Generating";
                        TxtAutoPilotBadge.Text = "AI ACTIVE";
                        TxtAutoPilotBadge.Foreground = (SolidColorBrush)FindResource("BrushTextWhite");
                    }
                    else
                    {
                        TxtTargetProcess.Text = "Manual Session";
                        TxtTargetDesc.Text = " • Clamshell Awake";
                        TxtAutoPilotBadge.Text = "ACTIVE";
                        TxtAutoPilotBadge.Foreground = (SolidColorBrush)FindResource("BrushTextWhite");
                    }
                    TxtHeaderSub.Text = "Lid-Close Awake • Active";
                }
                else
                {
                    DotStatus.Fill = (SolidColorBrush)FindResource("BrushTextDim");
                    TxtTargetProcess.Text = "Vigil Guard";
                    TxtTargetDesc.Text = " • Standby";
                    TxtAutoPilotBadge.Text = "AUTO-PILOT";
                    TxtAutoPilotBadge.Foreground = (SolidColorBrush)FindResource("BrushTextMuted");
                    TxtHeaderSub.Text = "Lid-Close Awake • Standby";
                }
            }
            finally
            {
                _isSyncingMasterSwitch = false;
            }

            // 3. Thermal State
            var temp = _thermal.CurrentCpuTempCelsius;
            if (temp > 0)
            {
                TxtTempDigits.Text = $"{temp:F0}°C";
            }
            TxtPillThrottle.Text = $"CAP {_thermal.CurrentMaxProcessorState}%";

            if (_thermal.IsEmergencyThrottled)
            {
                TxtTempStatus.Text = "Emergency Throttle!";
                TxtTempStatus.Foreground = (SolidColorBrush)FindResource("BrushTextWhite");
            }
            else
            {
                TxtTempStatus.Text = _thermal.CurrentCpuLoadPercent > 0 
                    ? $"{_thermal.CurrentCpuLoadPercent}% Load • Cold" 
                    : "Turbo OFF • Cold";
                TxtTempStatus.Foreground = (SolidColorBrush)FindResource("BrushTextSilver");
            }

            // 4. Wi-Fi
            TxtWifiSsid.Text = string.IsNullOrEmpty(_wifi.CurrentConnectedSsid) ? "Not Connected" : _wifi.CurrentConnectedSsid;
            if (ChkAutoWifi.IsChecked == true)
            {
                TxtWifiPing.Text = $"{_wifi.PingLatencyMs}ms • Auto-Reconnect ON";
                TxtWifiPing.Foreground = (SolidColorBrush)FindResource("BrushTextMuted");
            }
            else
            {
                TxtWifiPing.Text = "Auto-Reconnect OFF • Manual";
                TxtWifiPing.Foreground = (SolidColorBrush)FindResource("BrushTextDim");
            }

            // 5. Freezer
            if (_freezer.SuspendedProcessNames.Count > 0)
            {
                TxtFrozenApps.Text = string.Join(", ", _freezer.SuspendedProcessNames);
            }
            else
            {
                TxtFrozenApps.Text = "none";
            }

            // 6. Battery (via SystemInformation.PowerStatus)
            try
            {
                var power = System.Windows.Forms.SystemInformation.PowerStatus;
                var pct = (int)(power.BatteryLifePercent * 100);
                var isAc = power.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;

                TxtBatPct.Text = $"{pct}%";
                ProgressBattery.Value = pct;

                if (isAc)
                {
                    TxtBatStatus.Text = "⚡ AC Connected";
                    TxtBatStatus.Foreground = (SolidColorBrush)FindResource("BrushTextWhite");
                    ProgressBattery.Foreground = (SolidColorBrush)FindResource("BrushTextWhite");
                }
                else
                {
                    TxtBatStatus.Text = "🔋 On Battery";
                    TxtBatStatus.Foreground = (SolidColorBrush)FindResource("BrushTextWhite");
                    ProgressBattery.Foreground = pct <= 20 
                        ? (SolidColorBrush)FindResource("BrushTextMuted") 
                        : (SolidColorBrush)FindResource("BrushTextWhite");
                }

                if (power.BatteryLifeRemaining > 0 && !isAc)
                {
                    var hrs = power.BatteryLifeRemaining / 3600;
                    var mins = (power.BatteryLifeRemaining % 3600) / 60;
                    TxtBatRuntime.Text = $"~{hrs}h {mins}m left";
                }
                else
                {
                    TxtBatRuntime.Text = isAc ? "Full Capacity" : "~4h left";
                }
            }
            catch
            {
                // Fallback
            }
        });
    }

    private void Coordinator_StateChanged(object? sender, SessionStateChangedEventArgs e) => RefreshUi();
    private void Thermal_TemperatureUpdated(object? sender, float e) => RefreshUi();
    private void Wifi_StatusChanged(object? sender, WiFiStatusChangedEventArgs e) => RefreshUi();

    private void BtnMinimizeFlyout_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

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

    private void Header_MouseDown(object sender, MouseButtonEventArgs e) => Window_MouseDown(sender, e);

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
        await _coordinator.EndSessionAsync(SessionEndReason.UserRequested);
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
}
