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
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Vigil.Views;

public partial class SettingsWindow : Window
{
    private record QuickPreset(string Name, string[] ExeNames);

    private static readonly QuickPreset[] Presets = new[]
    {
        new QuickPreset("GTA V", new[] { "gta5.exe", "gta5_enhanced.exe" }),
        new QuickPreset("Unreal", new[] { "unrealeditor.exe" }),
        new QuickPreset("Unity", new[] { "unity.exe" }),
        new QuickPreset("Steam", new[] { "steam.exe" }),
        new QuickPreset("Epic", new[] { "epicgameslauncher.exe" }),
        new QuickPreset("Blender", new[] { "blender.exe" }),
        new QuickPreset("DaVinci", new[] { "resolve.exe" }),
        new QuickPreset("HandBrake", new[] { "handbrake.exe" }),
        new QuickPreset("OBS", new[] { "obs64.exe" }),
        new QuickPreset("Premiere", new[] { "adobe premiere pro.exe" }),
        new QuickPreset("ComfyUI", new[] { "webui.exe" })
    };

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
        RenderQuickPresets();
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
                RenderQuickPresets();
            };

            removeBtn.MouseEnter += (_, _) => removeBtn.Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0x53, 0x53));
            removeBtn.MouseLeave += (_, _) => removeBtn.Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x86, 0x96));

            stack.Children.Add(textBlock);
            stack.Children.Add(removeBtn);
            chipBorder.Child = stack;

            PnlProcessChips.Children.Add(chipBorder);
        }
    }

    private void RenderQuickPresets()
    {
        PnlQuickPresets.Children.Clear();
        var monoFont = TryFindResource("FontMono") as FontFamily ?? new FontFamily("Consolas, monospace");

        foreach (var preset in Presets)
        {
            var p = preset; // Closure copy
            var isAdded = p.ExeNames.All(exe => _workingProcesses.Any(wp => wp.Equals(exe, StringComparison.OrdinalIgnoreCase)));

            var btn = new Button
            {
                Content = isAdded ? $"✓ {p.Name}" : $"+ {p.Name}",
                Margin = new Thickness(0, 0, 5, 5),
                Padding = new Thickness(7, 3, 7, 3),
                FontSize = 10,
                FontWeight = isAdded ? FontWeights.Normal : FontWeights.SemiBold,
                FontFamily = monoFont,
                Background = isAdded 
                    ? new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF)) 
                    : new SolidColorBrush(Color.FromArgb(0x1C, 0x3B, 0x82, 0xF6)),
                Foreground = isAdded 
                    ? new SolidColorBrush(Color.FromRgb(0x60, 0x66, 0x75)) 
                    : new SolidColorBrush(Color.FromRgb(0x93, 0xC5, 0xFD)),
                BorderBrush = isAdded 
                    ? new SolidColorBrush(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF)) 
                    : new SolidColorBrush(Color.FromArgb(0x38, 0x3B, 0x82, 0xF6)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = isAdded 
                    ? $"{string.Join(", ", p.ExeNames)} (Click to remove)" 
                    : $"Add {string.Join(", ", p.ExeNames)}"
            };

            btn.Click += (_, _) =>
            {
                if (isAdded)
                {
                    foreach (var exe in p.ExeNames)
                    {
                        _workingProcesses.RemoveAll(wp => wp.Equals(exe, StringComparison.OrdinalIgnoreCase));
                    }
                }
                else
                {
                    foreach (var exe in p.ExeNames)
                    {
                        AddProcess(exe);
                    }
                }
                RenderProcessChips();
                RenderQuickPresets();
            };

            PnlQuickPresets.Children.Add(btn);
        }
    }

    private record RunningApp(string ExeName, string Title);

    private List<RunningApp> GetRunningApplications()
    {
        var list = new List<RunningApp>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var ignoreList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "svchost", "conhost", "csrss", "winlogon", "dwm", "sihost", "taskhostw",
            "fontdrvhost", "explorer", "vigil", "searchhost", "textinputhost",
            "applicationframehost", "shellexperiencehost", "systemsettings",
            "runtimebroker", "backgroundtaskhost", "ctfmon", "securityhealthservice",
            "securityhealthsystray", "wmiinetsrv", "wmiprvse", "cmd", "powershell",
            "tasklist", "idle", "system"
        };

        try
        {
            int currentSessionId;
            try { currentSessionId = Process.GetCurrentProcess().SessionId; }
            catch { currentSessionId = -1; }

            var processes = Process.GetProcesses();

            // 1. Session processes with active MainWindowTitle
            foreach (var p in processes)
            {
                try
                {
                    if (currentSessionId != -1 && p.SessionId != currentSessionId) continue;
                    if (ignoreList.Contains(p.ProcessName)) continue;

                    var title = p.MainWindowTitle;
                    if (string.IsNullOrWhiteSpace(title)) continue;

                    var exeName = p.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                        ? p.ProcessName.ToLowerInvariant()
                        : (p.ProcessName + ".exe").ToLowerInvariant();

                    if (seen.Add(exeName))
                    {
                        list.Add(new RunningApp(exeName, title.Trim()));
                    }
                }
                catch { }
            }

            // 2. Session processes without window title (e.g. background engines or child processes)
            foreach (var p in processes)
            {
                try
                {
                    if (currentSessionId != -1 && p.SessionId != currentSessionId) continue;
                    if (ignoreList.Contains(p.ProcessName)) continue;

                    var exeName = p.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                        ? p.ProcessName.ToLowerInvariant()
                        : (p.ProcessName + ".exe").ToLowerInvariant();

                    if (seen.Add(exeName))
                    {
                        list.Add(new RunningApp(exeName, string.Empty));
                    }
                }
                catch { }
            }
        }
        catch { }

        return list;
    }

    private void BtnRunningApps_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu
        {
            Style = TryFindResource("DarkContextMenuStyle") as Style,
            PlacementTarget = BtnRunningApps,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom
        };

        var runningApps = GetRunningApplications();
        if (runningApps.Count == 0)
        {
            var emptyItem = new MenuItem
            {
                Header = "(No active windowed apps detected)",
                IsEnabled = false,
                Style = TryFindResource("DarkMenuItemStyle") as Style
            };
            menu.Items.Add(emptyItem);
        }
        else
        {
            foreach (var app in runningApps)
            {
                var isAdded = _workingProcesses.Any(p => p.Equals(app.ExeName, StringComparison.OrdinalIgnoreCase));
                var displayTitle = string.IsNullOrWhiteSpace(app.Title) ? app.ExeName : app.Title;
                if (displayTitle.Length > 28)
                {
                    displayTitle = displayTitle.Substring(0, 26) + "...";
                }

                var item = new MenuItem
                {
                    Header = isAdded ? $"✓  {displayTitle}" : $"+  {displayTitle}",
                    InputGestureText = app.ExeName,
                    IsEnabled = !isAdded,
                    Style = TryFindResource("DarkMenuItemStyle") as Style
                };

                var exe = app.ExeName;
                item.Click += (_, _) =>
                {
                    AddProcess(exe);
                };

                menu.Items.Add(item);
            }
        }

        menu.IsOpen = true;
    }

    private void BtnBrowseExe_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Application Executable",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = true
        };

        if (dlg.ShowDialog(this) == true)
        {
            foreach (var file in dlg.FileNames)
            {
                var fileName = Path.GetFileName(file);
                AddProcess(fileName);
            }
        }
    }

    private void AddProcessFromInput()
    {
        var input = TxtNewProcess.Text.Trim();
        if (string.IsNullOrWhiteSpace(input)) return;

        AddProcess(input);
        TxtNewProcess.Text = string.Empty;
    }

    private void AddProcess(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        // Clean up input
        input = input.Replace(" ", "");
        if (!input.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            input += ".exe";
        }
        input = input.ToLowerInvariant();

        if (!_workingProcesses.Any(p => p.Equals(input, StringComparison.OrdinalIgnoreCase)))
        {
            _workingProcesses.Add(input);
            RenderProcessChips();
            RenderQuickPresets();
        }
    }

    private void BtnAddProcess_Click(object sender, RoutedEventArgs e)
    {
        AddProcessFromInput();
    }

    private void TxtNewProcess_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (TxtPlaceholder != null)
        {
            TxtPlaceholder.Visibility = string.IsNullOrEmpty(TxtNewProcess.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
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
        RenderQuickPresets();
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
