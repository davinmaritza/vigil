# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2026-11-19

### Added
- **Visual Settings Window**: Added an interactive modal settings interface (`SettingsWindow`) accessible via the gear button `⚙` in the menu bar popover or system tray context menu.
- **Visual Sliders & Toggles**: Intuitive controls for Max Processor State (70-100%), Emergency Thermal Threshold (65-95°C), Start on boot, Screen sleep behavior, Mute audio on lid close, Wi-Fi watchdog, Process freezer, and Auto-pilot.
- **Direct Configuration Access**: Integrated `📄 Edit config.json` shortcut button that opens `%LOCALAPPDATA%\Vigil\config.json` directly in Notepad for power users.
- **Settings Persistence**: Implemented `VigilConfig.Save()` and `GetConfigPath()` helpers with indented JSON serialization and automatic directory creation.

### Changed
- **Menu Bar Header Gear Icon**: Added a discrete settings gear button `[⚙]` in the compact flyout header bar next to the close button.
- **System Tray Menu**: Added `⚙ Settings...` menu item for immediate access to configuration from the taskbar notification area.
- **Documentation & UI Showcase**: Overhauled Configuration section with an intuitive options guide and table, eliminating reliance on raw JSON editing, and added visual showcases of Active, Standby, and Settings views.

## [1.0.1] - 2026-11-19

### Changed
- **Compact Menu Bar Popover**: Redesigned flyout into an ultra-compact 268 × 208 px menu bar popover inspired by Insomnia macOS, replacing the oversized dashboard with an unobtrusive taskbar flyout.
- **Minimalist Layout**: Stripped cramped double-nested card borders, thick battery progress bar, and warning hint banners in favor of flat rows with centered timer and 1-line micro telemetry.
- **100% English Localization**: Replaced all Indonesian terminology across UI controls, status labels, tooltips, and tray menus with clean, idiomatic English (`+30m`, `Stop (ESC)`, `System Standby`, `Clamshell Guard Active`, `Start on boot`).
- **Safe Display Handling**: Changed default `turnDisplayOffOnSession = false` so screens never turn black unexpectedly on session start while the laptop is actively in use on a desk. Backlight sleeps naturally only upon physical clamshell lid closure.
- **Instant Display Wake**: Added a 1px synthetic mouse nudge to `WakeScreen()` to ensure Windows display drivers immediately restore backlight on lid open or session termination.

### Added
- **Fail-Safe Session Cancellation**: Prominent vermillion `Stop (ESC)` button in flyout window, direct keyboard `ESC` shortcut, top-right master `OFF` switch, and top-level tray menu stop command.
- **Interactive Preset Chips**: Clean 5-column preset buttons (`30m`, `1h`, `2h`, `4h`, `∞`) for instant session arming.

## [1.0.0] - 2026-11-18

### Added
- **Master ON / OFF Protection Switch**: Tactile toggle on the flyout header strip allowing instant one-click activation and deactivation without setting timers.
- **AI Workload Auto-Pilot**: Background process observer detecting active AI pipelines (`antigravity.exe`, `claude.exe`, `cursor.exe`, `ollama.exe`, `python.exe`, `blender.exe`, `lmstudio.exe`, `jan.exe`), auto-arming into `GUARD ACTIVE` mode and returning cleanly to Standby on completion.
- **Wi-Fi Auto-Reconnect Watchdog**: Dedicated toggle switch and dynamic ping latency monitor re-associating dropped wireless profiles via `netsh` during headless lid-closed operations.
- **Industrial Minimalist Design System**: Compact popover inspired by Dieter Rams, Teenage Engineering, and Insomnia macOS; unified container with hairline dividers, Moon-White on Charcoal typography, and precision hardware switches.
- **Standalone Single-File Executable**: Self-extracting single binary `Vigil.exe` published alongside portable zip archive on GitHub Releases.
- **Clamshell Protection**: Prevents sleep when laptop lid is closed via `powercfg` and `SetThreadExecutionState`.
- **Thermal Throttler**: Enforces 92% maximum processor state (`PROCTHROTTLEMAX`) to eliminate Turbo Boost voltage surges.
- **Process Freezer**: Suspends heavy idle background apps (Slack, Discord, Spotify) via `NtSuspendProcess`.
- **Crash Recovery Journal**: Atomic write-ahead journal (`recovery.json`) and Windows Task Scheduler watchdog backstop.
- **System Tray & Shell Integration**: Minimize-to-tray, draggable canvas, and crisp high-contrast white tray icon (`tray-icon-32.png`).
- **CLI Automation**: Full terminal scripting support (`--session status --json`, `--session start`, `--recover`).
