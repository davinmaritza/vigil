using System.Windows;
using System.Windows.Media;
using Vigil.Core;
using Vigil.Core.Contracts;
using Vigil.Native;

namespace Vigil.Views;

public partial class FlyoutWindow : Window
{
    private readonly ISessionCoordinator _coordinator;
    private readonly IThermalThrottler _thermal;
    private readonly IWiFiWatchdog _wifi;
    private readonly IProcessFreezer _freezer;
    private bool _isInitializing = true;

    public FlyoutWindow(
        ISessionCoordinator coordinator,
        IThermalThrottler thermal,
        IWiFiWatchdog wifi,
        IProcessFreezer freezer)
    {
        InitializeComponent();

        _coordinator = coordinator;
        _thermal = thermal;
        _wifi = wifi;
        _freezer = freezer;

        _coordinator.StateChanged += Coordinator_StateChanged;
        _thermal.TemperatureUpdated += Thermal_TemperatureUpdated;
        _wifi.StatusChanged += Wifi_StatusChanged;

        // Startup auto-launch checkbox initial state
        ChkAutoStart.IsChecked = StartupManager.IsRunOnStartupEnabled();
        ChkAutoStart.Checked += (_, _) => StartupManager.SetRunOnStartup(true);
        ChkAutoStart.Unchecked += (_, _) => StartupManager.SetRunOnStartup(false);

        // Wi-Fi watchdog checkbox
        ChkAutoWifi.IsChecked = _wifi.IsApplied;
        ChkAutoWifi.Checked += async (_, _) =>
        {
            if (!_isInitializing) await _wifi.ApplyAsync();
        };
        ChkAutoWifi.Unchecked += async (_, _) =>
        {
            if (!_isInitializing) await _wifi.RevertAsync();
        };

        _isInitializing = false;

        PositionNearTray();
        RefreshUi();
    }

    public void PositionNearTray()
    {
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

            // 2. Status Strip & Dot
            if (_coordinator.CurrentStatus == SessionStatus.Active)
            {
                DotStatus.Fill = (SolidColorBrush)FindResource("BrushAccentEmerald");
                TxtTargetProcess.Text = _coordinator.ActiveTargetProcess ?? "Manual Session";
                TxtTargetDesc.Text = " • Vigil Active";
                TxtAutoPilotBadge.Text = "ACTIVE";
                TxtHeaderSub.Text = "Lid-Close Awake • Display Off";
            }
            else
            {
                DotStatus.Fill = (SolidColorBrush)FindResource("BrushTextDim");
                TxtTargetProcess.Text = "Standby";
                TxtTargetDesc.Text = " • System Normal";
                TxtAutoPilotBadge.Text = "IDLE";
                TxtHeaderSub.Text = "No Session Running";
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
                TxtTempStatus.Foreground = (SolidColorBrush)FindResource("BrushAccentRose");
            }
            else
            {
                TxtTempStatus.Text = _thermal.CurrentCpuLoadPercent > 0 
                    ? $"{_thermal.CurrentCpuLoadPercent}% Load • Cold" 
                    : "Turbo OFF • Cold";
                TxtTempStatus.Foreground = (SolidColorBrush)FindResource("BrushAccentEmerald");
            }

            // 4. Wi-Fi
            TxtWifiSsid.Text = _wifi.CurrentConnectedSsid;
            TxtWifiPing.Text = $"{_wifi.PingLatencyMs}ms • Watchdog On";

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
                    TxtBatStatus.Foreground = (SolidColorBrush)FindResource("BrushAccentEmerald");
                    ProgressBattery.Foreground = (SolidColorBrush)FindResource("BrushAccentEmerald");
                }
                else
                {
                    TxtBatStatus.Text = "🔋 On Battery";
                    if (pct <= 20)
                    {
                        TxtBatStatus.Foreground = (SolidColorBrush)FindResource("BrushAccentRose");
                        ProgressBattery.Foreground = (SolidColorBrush)FindResource("BrushAccentRose");
                    }
                    else if (pct <= 40)
                    {
                        TxtBatStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)); // Amber
                        ProgressBattery.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11));
                    }
                    else
                    {
                        TxtBatStatus.Foreground = (SolidColorBrush)FindResource("BrushAccentLime");
                        ProgressBattery.Foreground = (SolidColorBrush)FindResource("BrushAccentLime");
                    }
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

    private void BtnCloseFlyout_Click(object sender, RoutedEventArgs e)
    {
        Hide();
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
