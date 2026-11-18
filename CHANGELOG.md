# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-11-18

### Added
- **Master ON / OFF Protection Switch**: Tactile toggle on the flyout header strip allowing instant one-click activation and deactivation without setting timers.
- **AI Workload Auto-Pilot**: Background process observer detecting active AI pipelines (`antigravity.exe`, `claude.exe`, `cursor.exe`, `ollama.exe`, `python.exe`, `blender.exe`, `lmstudio.exe`, `jan.exe`), auto-arming into `AI ACTIVE` mode and returning cleanly to Standby on completion.
- **Wi-Fi Auto-Reconnect Watchdog**: Dedicated toggle switch and dynamic ping latency monitor re-associating dropped wireless profiles via `netsh` during headless lid-closed operations.
- **Industrial Minimalist Design System**: Compact 316×465 px popover inspired by Dieter Rams, Teenage Engineering, and Insomnia macOS; unified Grouped Inset container with hairline dividers, Moon-White on Charcoal typography, and precision hardware switches.
- **Standalone Single-File Executable**: Self-extracting single binary `Vigil.exe` published alongside portable zip archive on GitHub Releases.
- **Clamshell Protection**: Prevents sleep when laptop lid is closed via `powercfg` and `SetThreadExecutionState`.
- **Display Blackout**: Auto-turns off display panel backlight upon lid close to conserve battery and eliminate heat.
- **Thermal Throttler**: Enforces 92% maximum processor state (`PROCTHROTTLEMAX`) to eliminate Turbo Boost voltage surges.
- **Process Freezer**: Suspends heavy idle background apps (Slack, Discord, Spotify) via `NtSuspendProcess`.
- **Crash Recovery Journal**: Atomic write-ahead journal (`recovery.json`) and Windows Task Scheduler watchdog backstop.
- **System Tray & Shell Integration**: Minimize-to-tray, draggable canvas, and crisp high-contrast white tray icon (`tray-icon-32.png`).
- **CLI Automation**: Full terminal scripting support (`--session status --json`, `--session start`, `--recover`).
