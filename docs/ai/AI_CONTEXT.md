# AI Context: Vigil (Windows Clamshell & Cooling Guard)

> **Context Snapshot for Cross-AI Handoff & Session Recovery**
>
> - **Project**: Vigil
> - **Root Path**: `c:\Users\Despair\Projects\Web_and_AI\insomnia`
> - **Notion Root Page**: [Vigil](https://app.notion.com/p/41c13039c767810f9264c52276a32a95?pvs=204) (ID: `41c13039-c767-810f-9264-c52276a32a95`)
> - **Last Context Update**: 2026-11-18T13:45:00+07:00
> - **Last Successful Notion Sync**: 2026-11-18T13:35:00+07:00
> - **Sync Status**: 100% Synchronized (19 of 19 Knowledge Sections Live in Notion)

---

## 1. Current Objective
Build and maintain an integrated Project Knowledge System connecting the Vigil repository, vendor-neutral project rules (`docs/ai/PROJECT_RULES.md`), native AI instructions (`AGENTS.md`), and Notion.

## 2. Current Work
Pure monochrome (Black & White) design system overhaul, window minimization control, draggable window header, and taskbar window integration completed.

## 3. Last Completed Work
- **Pure Monochrome (Black & White) Design Overhaul**:
  - Replaced all colorful accents (cyan, lime, emerald, rose, amber) with high-contrast monochrome design tokens (`#0B0C0E` base, `#13141B` card, `#FFFFFF` pure white text/active states, `#D4D7E0` silver, `#868C9E` muted, `#4A5060` dim).
  - Redesigned tactile `BentoToggleSwitch` into high-contrast monochrome: dark track with gray thumb when off; pure white track with deep obsidian thumb when active.
  - Redesigned battery progress bar to pure white fill with subtle dark track.
  - End session button (`BentoDangerBtn`) transitioned to clean monochrome bordered button.
  - Fallback tray icon procedurally updated to pure white.
- **Window Minimize & Shell Integration**:
  - Added dedicated Minimize button (`BtnMinimizeFlyout`) in header with clean horizontal bar glyph alongside Close button (`BtnCloseFlyout`).
  - Enabled `ResizeMode="CanMinimize"` and `ShowInTaskbar="True"` for seamless taskbar window switching.
  - Added draggable window header (`Header_MouseDown` -> `DragMove()`).
  - Integrated un-minimize and activate logic when toggled from System Tray or Taskbar.
- **Brand Identity & Iconography (Concept 2 Selected)**:
  - Fused avant-garde typographic monogram with aerodynamic orbital gyro ring and central 4-pointed sentinel star flare.
  - Generated multi-resolution Windows icon (`assets/icon.ico` & `src/Vigil/icon.ico`: 256, 128, 64, 48, 32, 16px).
  - Integrated 64x64 Concept 2 logo directly into `FlyoutWindow.xaml` header and system tray `NotifyIcon`.
  - Re-published standalone release binary to `dist/Vigil.exe`.
  - Humanized `README.md` highlighting developer workflows with Antigravity, Claude Code, and local AI agent pipelines.
- **QA Hardening & Edge-Case Remediation**:
  - **Registry Fallback**: Implemented direct in-process registry reading in `PowerCfgRunner.cs` for Windows Modern Standby / OEM systems where `Attributes = 1` hides `LIDACTION` and `PROCTHROTTLEMAX` from `powercfg /query`.
  - **WAL Snapshot Ordering**: In `SessionCoordinator.cs`, ensured system power settings (`AcLidAction`, `DcLidAction`, `AcMaxProcessorState`, `DcMaxProcessorState`) are captured and persisted to `recovery.json` *prior* to modifying power states.
  - **CTS Re-usability**: Fixed `ThermalThrottler.cs` and `WiFiWatchdog.cs` cancellation token lifecycles so subsequent sessions can be started and stopped repeatedly without dead loops.
  - **Self-Freeze & System Protection**: Added `Environment.ProcessId` bypass and extended `SystemBlacklist` (`vigil`, `dotnet`, `conhost`, `cmd`, `powershell`, `pwsh`, `devenv`, `taskmgr`) in `ProcessFreezer.cs`.
  - **SSID Parsing Resilience**: Enhanced `WiFiWatchdog.cs` to safely extract SSIDs containing colons (e.g., `Home:5GHz`) and sanitize profile arguments against injection.
  - **Scheduler Localization**: Adapted `RecoveryJournal.cs` to query `CurrentCulture.DateTimeFormat.ShortDatePattern` and redirected stderr to prevent console pollution.
- **Test Suite Expansion**:
  - Created and expanded test suites across `PowerCfgRunnerFallbackTests.cs`, `WiFiWatchdogTests.cs`, `StartupManagerTests.cs`, `ProcessFreezerSafetyFilterTests.cs`, `RecoveryJournalTests.cs`, and `SessionCoordinatorTests.cs`.
  - Verified 38/38 unit and integration tests passing (0 warnings, 0 errors).
- **CLI & Release Validation**:
  - Published self-contained release to `dist/`.
  - Verified live execution: `--help`, `--session status --json`, and simulated crash `--recover` with atomic journal cleanup and power state restoration.

## 4. Next Action
Synchronize Notion pages `11. Testing`, `12. Known Issues & Technical Debt`, and `18. AI Context` with QA hardening outcomes.

## 5. Active Constraints
- **Zero AI Slop**: Bento Widget visual design tokens; `Plus Jakarta Sans` for UI copy; `JetBrains Mono` for telemetry digits.
- **Safety Invariants**:
  - Always write state to `%LOCALAPPDATA%\Vigil\recovery.json` using atomic `File.Replace`.
  - Maintain the `schtasks.exe` watchdog backstop (`expectedEnd + 1 minute`) for out-of-process crash recovery.
  - Keep CPU throttling state capped at 92% maximum processor state (`PROCTHROTTLEMAX`) during active sessions.
- **Truth Hierarchy**: Repository source code is the implementation truth; Notion is the project knowledge, architectural decisions, and memory truth.

## 6. Important Decisions (ADRs)
- **ADR-001**: Win32 P/Invoke & `powercfg` with Windows Registry fallback for Modern Standby systems.
- **ADR-002**: Turbo Boost suppression capped at 92% processor state to drop temperatures by 25–30°C.
- **ADR-003**: Two-tier write-ahead crash journaling with Windows Task Scheduler watchdog.
- **ADR-004**: Native NT API `NtSuspendProcess` for thread-safe background process freezing.

## 7. Active Files
- `src/Vigil/Native/PowerCfgRunner.cs`: Power policy runner with in-process registry fallback.
- `src/Vigil/Core/SessionCoordinator.cs`: Central state machine and write-ahead journaling coordinator.
- `src/Vigil/Core/PowerController.cs`: Power snapshotting and clamshell mode enforcer.
- `src/Vigil/Core/ThermalThrottler.cs`: WMI multi-tier thermal sensor polling with dynamic CTS.
- `src/Vigil/Core/WiFiWatchdog.cs`: ICMP ping probe, profile reconnect, and colon-safe parser.
- `src/Vigil/Core/ProcessFreezer.cs`: Safe process suspension with system blacklist and self-PID bypass.
- `src/Vigil/Core/RecoveryJournal.cs`: Atomic file replacement and localized task watchdog.
- `src/Vigil/App.xaml.cs`: Mutex, Tray icon, CLI router, crash recovery check.
- `src/Vigil/Views/FlyoutWindow.xaml`: Bento Grid XAML layout.
- `tests/Vigil.Tests/`: 38 unit & integration tests covering core modules.

## 8. Known Issues & Technical Debt
- **TECH-001**: Discrete GPU (NVIDIA NVML) temperature polling planned for Milestone 1.1.
- No active blockers.

## 9. Do Not Change
- Do not eliminate the `schtasks.exe` watchdog backstop in `RecoveryJournal.cs`.
- Do not raise the CPU active throttling cap above 92% (firmware will surge Turbo Boost voltages if ceiling exceeds 95%).
- Do not remove the `Environment.ProcessId` or `SystemBlacklist` checks in `ProcessFreezer.cs`.

## 10. Last Validation
- **Unit Tests**: `dotnet test tests/Vigil.Tests/Vigil.Tests.csproj` -> 38 Passed, 0 Failed, 0 Warnings (449 ms).
- **CLI Telemetry**: `dist/Vigil.exe --session status --json` -> Exit code 0, telemetry verified.
- **Crash Recovery Validation**: Verified simulated active journal recovery restores power indices and deletes `%LOCALAPPDATA%\Vigil\recovery.json`.

---

## 11. Notion Knowledge Base Page Directory

| Section | Notion Page URL | Page ID |
| :--- | :--- | :--- |
| **Vigil (Root)** | [Vigil Root](https://app.notion.com/p/41c13039c767810f9264c52276a32a95?pvs=204) | `41c13039-c767-810f-9264-c52276a32a95` |
| **00. Project Overview** | [00. Project Overview](https://app.notion.com/p/41c13039c76781a9bf12e907baf15038?pvs=204) | `41c13039-c767-81a9-bf12-e907baf15038` |
| **01. Current State** | [01. Current State](https://app.notion.com/p/41c13039c7678195a66bd4dfd411ee37?pvs=204) | `41c13039-c767-8195-a66b-d4dfd411ee37` |
| **02. Architecture** | [02. Architecture](https://app.notion.com/p/41c13039c76781a3ab6ce0a7623fbd1c?pvs=204) | `41c13039-c767-81a3-ab6c-e0a7623fbd1c` |
| **03. Technical Context** | [03. Technical Context](https://app.notion.com/p/41c13039c76781db9e70d5c42cd5d549?pvs=204) | `41c13039-c767-81db-9e70-d5c42cd5d549` |
| **04. Product Context** | [04. Product Context](https://app.notion.com/p/41c13039c76781d98d7fe530d1d900ca?pvs=204) | `41c13039-c767-81d9-8d7f-e530d1d900ca` |
| **05. Project Decisions** | [05. Project Decisions](https://app.notion.com/p/41c13039c7678100954fc7e16cf4c4fa?pvs=204) | `41c13039-c767-8100-954f-c7e16cf4c4fa` |
| **06. Development Rules** | [06. Development Rules](https://app.notion.com/p/41c13039c7678102814bc3430c375246?pvs=204) | `41c13039-c767-8102-814b-c3430c375246` |
| **07. Features & Modules** | [07. Features & Modules](https://app.notion.com/p/41c13039c76781819a94c707aad8c2cf?pvs=204) | `41c13039-c767-8181-9a94-c707aad8c2cf` |
| **08. Database & Data Model** | [08. Database & Data Model](https://app.notion.com/p/41c13039c76781f78310fe93c76b8f04?pvs=204) | `41c13039-c767-81f7-8310-fe93c76b8f04` |
| **09. API & Integrations** | [09. API & Integrations](https://app.notion.com/p/41c13039c7678103a8c0cb920a6fa2e8?pvs=204) | `41c13039-c767-8103-a8c0-cb920a6fa2e8` |
| **10. Security** | [10. Security](https://app.notion.com/p/41c13039c76781ba9bc1cdaa23693360?pvs=204) | `41c13039-c767-81ba-9bc1-cdaa23693360` |
| **11. Testing** | [11. Testing](https://app.notion.com/p/41c13039c767819ab12af2d0d0e988fc?pvs=204) | `41c13039-c767-819a-b12a-f2d0d0e988fc` |
| **12. Known Issues & Tech Debt**| [12. Known Issues & Tech Debt](https://app.notion.com/p/41c13039c76781078c75d3a75950f31f?pvs=204) | `41c13039-c767-8107-8c75-d3a75950f31f` |
| **13. Progress & Milestones** | [13. Progress & Milestones](https://app.notion.com/p/41c13039c767817f8847db6e257e8942?pvs=204) | `41c13039-c767-817f-8847-db6e257e8942` |
| **14. Changelog** | [14. Changelog](https://app.notion.com/p/41c13039c7678123bc1bfac9ddf38e6d?pvs=204) | `41c13039-c767-8123-bc1b-fac9ddf38e6d` |
| **15. Project Memory** | [15. Project Memory](https://app.notion.com/p/41c13039c767819f83b5ea2f03b1cef4?pvs=204) | `41c13039-c767-819f-83b5-ea2f03b1cef4` |
| **16. Documentation** | [16. Documentation](https://app.notion.com/p/41c13039c76781aa8886f5456828a882?pvs=204) | `41c13039-c767-81aa-8886-f5456828a882` |
| **17. Tasks & Next Steps** | [17. Tasks & Next Steps](https://app.notion.com/p/41c13039c76781ddaa63f649e90718da?pvs=204) | `41c13039-c767-81dd-aa63-f649e90718da` |
| **18. AI Context** | [18. AI Context](https://app.notion.com/p/41c13039c76781e19ba6c24b0b3b2642?pvs=204) | `41c13039-c767-81e1-9ba6-c24b0b3b2642` |
