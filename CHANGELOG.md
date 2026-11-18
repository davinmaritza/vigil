# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2026-11-19

### Changed
- **Minimalist & Spacious UI Overhaul**: Redesigned flyout popover to a clean, breathable 320 × 395 px layout inspired by Dieter Rams and Teenage Engineering. Stripped cramped double-nested card borders, thick battery progress bar, and warning hint banners in favor of flat rows separated by delicate hairlines.
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
