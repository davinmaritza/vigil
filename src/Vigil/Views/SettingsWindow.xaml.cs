using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Vigil.Models;
using Vigil.Native;

namespace Vigil.Views;

public partial class SettingsWindow : Window
{
    private readonly VigilConfig _config;

    public SettingsWindow(VigilConfig config)
    {
        InitializeComponent();
        _config = config;

        LoadSettings();
        PreviewKeyDown += SettingsWindow_PreviewKeyDown;
    }

    private void LoadSettings()
    {
        ChkStartWithWindows.IsChecked = StartupManager.IsRunOnStartupEnabled();
        ChkKeepScreenOn.IsChecked = !_config.General.TurnDisplayOffOnSession;
        ChkMuteAudio.IsChecked = _config.General.MuteAudioOnLidClose;

        var defaultMins = Math.Clamp(_config.General.DefaultDurationSeconds / 60, 15, 480);
        SliderDefaultDuration.Value = defaultMins;
        UpdateDefaultDurationLabel(defaultMins);

        SliderMaxProcessor.Value = _config.Thermal.MaxProcessorStatePercent;
        TxtMaxProcessorLabel.Text = $"{_config.Thermal.MaxProcessorStatePercent}%";

        SliderEmergencyTemp.Value = _config.Thermal.EmergencyTempThresholdCelsius;
        TxtEmergencyTempLabel.Text = $"{_config.Thermal.EmergencyTempThresholdCelsius}°C";

        ChkWifiWatchdog.IsChecked = _config.WiFi.WatchdogEnabled;
        ChkProcessFreezer.IsChecked = _config.Freezer.Enabled;
        ChkAutoPilot.IsChecked = _config.AutoPilot.Enabled;
    }

    private void SliderDefaultDuration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateDefaultDurationLabel((int)e.NewValue);
    }

    private void UpdateDefaultDurationLabel(int totalMins)
    {
        if (TxtDefaultDurationLabel != null)
        {
            var h = totalMins / 60;
            var m = totalMins % 60;
            TxtDefaultDurationLabel.Text = h > 0 ? $"{h}h {m:D2}m" : $"{m}m";
        }
    }

    private void SliderMaxProcessor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtMaxProcessorLabel != null)
        {
            TxtMaxProcessorLabel.Text = $"{(int)e.NewValue}%";
        }
    }

    private void SliderEmergencyTemp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtEmergencyTempLabel != null)
        {
            TxtEmergencyTempLabel.Text = $"{(int)e.NewValue}°C";
        }
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var startWithWindows = ChkStartWithWindows.IsChecked == true;
        StartupManager.SetRunOnStartup(startWithWindows);
        _config.General.StartWithWindows = startWithWindows;

        _config.General.TurnDisplayOffOnSession = ChkKeepScreenOn.IsChecked != true;
        _config.General.MuteAudioOnLidClose = ChkMuteAudio.IsChecked == true;
        _config.General.DefaultDurationSeconds = (int)SliderDefaultDuration.Value * 60;

        _config.Thermal.MaxProcessorStatePercent = (int)SliderMaxProcessor.Value;
        _config.Thermal.EmergencyTempThresholdCelsius = (int)SliderEmergencyTemp.Value;

        _config.WiFi.WatchdogEnabled = ChkWifiWatchdog.IsChecked == true;
        _config.Freezer.Enabled = ChkProcessFreezer.IsChecked == true;
        _config.AutoPilot.Enabled = ChkAutoPilot.IsChecked == true;

        _config.Save();

        Close();
    }

    private void BtnOpenJson_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = VigilConfig.GetConfigPath();
            if (!File.Exists(path))
            {
                _config.Save();
            }

            Process.Start(new ProcessStartInfo("notepad.exe", path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Unable to open configuration file: {ex.Message}", "Vigil", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SettingsWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
