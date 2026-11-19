using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Vigil.Models;
using Vigil.Native;

using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Orientation = System.Windows.Controls.Orientation;
using Brushes = System.Windows.Media.Brushes;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Vigil.Views;

public partial class SettingsWindow : Window
{
    private readonly VigilConfig _config;
    private readonly List<string> _workingProcesses = new();

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

        // Battery Floor Guard
        ChkBatteryGuard.IsChecked = _config.Battery.BatteryGuardEnabled;
        SliderLowBattery.Value = Math.Clamp(_config.Battery.LowBatteryThresholdPercent, 10, 40);
        TxtLowBatteryLabel.Text = $"{_config.Battery.LowBatteryThresholdPercent}%";

        // Thermal ceilings
        SliderMaxProcessor.Value = _config.Thermal.MaxProcessorStatePercent;
        TxtMaxProcessorLabel.Text = $"{_config.Thermal.MaxProcessorStatePercent}%";

        SliderEmergencyTemp.Value = _config.Thermal.EmergencyTempThresholdCelsius;
        TxtEmergencyTempLabel.Text = $"{_config.Thermal.EmergencyTempThresholdCelsius}°C";

        // Network & Workloads
        ChkWifiWatchdog.IsChecked = _config.WiFi.WatchdogEnabled;
        ChkProcessFreezer.IsChecked = _config.Freezer.Enabled;
        ChkAutoPilot.IsChecked = _config.AutoPilot.Enabled;

        // Auto-Pilot Monitored Processes
        _workingProcesses.Clear();
        _workingProcesses.AddRange(_config.AutoPilot.WatchProcesses);
        RenderProcessChips();
    }

    private void RenderProcessChips()
    {
        PnlProcessChips.Children.Clear();
        var monoFont = TryFindResource("FontMono") as FontFamily ?? new FontFamily("Consolas, monospace");

        foreach (var proc in _workingProcesses)
        {
            var pName = proc; // Closure copy
            var chipBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1D, 0x1F, 0x25)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(6, 2, 4, 2),
                Margin = new Thickness(0, 0, 5, 5)
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal };

            var textBlock = new TextBlock
            {
                Text = pName,
                FontFamily = monoFont,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xF2)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var removeBtn = new Button
            {
                Content = "×",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x86, 0x96)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Padding = new Thickness(2, 0, 2, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = $"Remove {pName}"
            };

            removeBtn.Click += (_, _) =>
            {
                _workingProcesses.Remove(pName);
                RenderProcessChips();
            };

            removeBtn.MouseEnter += (_, _) => removeBtn.Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0x53, 0x53));
            removeBtn.MouseLeave += (_, _) => removeBtn.Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x86, 0x96));

            stack.Children.Add(textBlock);
            stack.Children.Add(removeBtn);
            chipBorder.Child = stack;

            PnlProcessChips.Children.Add(chipBorder);
        }
    }

    private void AddProcessFromInput()
    {
        var input = TxtNewProcess.Text.Trim();
        if (string.IsNullOrWhiteSpace(input)) return;

        // Clean up input
        input = input.Replace(" ", "");
        if (!input.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            input += ".exe";
        }

        if (!_workingProcesses.Any(p => p.Equals(input, StringComparison.OrdinalIgnoreCase)))
        {
            _workingProcesses.Add(input.ToLowerInvariant());
            RenderProcessChips();
        }

        TxtNewProcess.Text = string.Empty;
    }

    private void BtnAddProcess_Click(object sender, RoutedEventArgs e)
    {
        AddProcessFromInput();
    }

    private void TxtNewProcess_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddProcessFromInput();
            e.Handled = true;
        }
    }

    private void BtnResetProcesses_Click(object sender, RoutedEventArgs e)
    {
        _workingProcesses.Clear();
        _workingProcesses.AddRange(new[]
        {
            "antigravity.exe",
            "claude.exe",
            "cursor.exe",
            "ollama.exe",
            "python.exe",
            "blender.exe",
            "lmstudio.exe",
            "jan.exe"
        });
        RenderProcessChips();
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

    private void SliderLowBattery_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtLowBatteryLabel != null)
        {
            TxtLowBatteryLabel.Text = $"{(int)e.NewValue}%";
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

        // Battery Guard
        _config.Battery.BatteryGuardEnabled = ChkBatteryGuard.IsChecked == true;
        _config.Battery.LowBatteryThresholdPercent = (int)SliderLowBattery.Value;

        // Thermal Ceilings
        _config.Thermal.MaxProcessorStatePercent = (int)SliderMaxProcessor.Value;
        _config.Thermal.EmergencyTempThresholdCelsius = (int)SliderEmergencyTemp.Value;

        // Network & Workloads
        _config.WiFi.WatchdogEnabled = ChkWifiWatchdog.IsChecked == true;
        _config.Freezer.Enabled = ChkProcessFreezer.IsChecked == true;
        _config.AutoPilot.Enabled = ChkAutoPilot.IsChecked == true;
        _config.AutoPilot.WatchProcesses = new List<string>(_workingProcesses);

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
