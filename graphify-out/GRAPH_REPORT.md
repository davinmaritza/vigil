# Graph Report - insomnia  (2026-11-18)

## Corpus Check
- Corpus is ~9,518 words - fits in a single context window. You may not need a graph.

## Summary
- 356 nodes · 742 edges · 13 communities
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 83 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Session Coordination & Timer
- C# Namespaces & Types
- Configuration & Auto-Pilot Models
- Win32 PInvoke & Native Interop
- Application Entry & System Tray
- Fluent Bento UI Flyout
- Process Watcher & Freezer
- Session State Contracts
- Write-Ahead Recovery Journal
- System Modifier Lifecycle
- Thermal Throttling Subsystem
- Wi-Fi Watchdog & Networking
- NuGet Packages & Build Dependencies

## God Nodes (most connected - your core abstractions)
1. `SessionCoordinator` - 24 edges
2. `App` - 23 edges
3. `Window` - 23 edges
4. `WiFiWatchdog` - 22 edges
5. `VigilConfig` - 21 edges
6. `ThermalThrottler` - 20 edges
7. `RecoveryData` - 20 edges
8. `ProcessFreezer` - 18 edges
9. `FlyoutWindow` - 18 edges
10. `PowerController` - 15 edges

## Surprising Connections (you probably didn't know these)
- `App` --inherits--> `Application`  [EXTRACTED]
  src/Vigil/App.xaml.cs → src/Vigil/App.xaml
- `App` --references--> `PowerController`  [EXTRACTED]
  src/Vigil/App.xaml.cs → src/Vigil/Core/PowerController.cs
- `App` --references--> `ProcessFreezer`  [EXTRACTED]
  src/Vigil/App.xaml.cs → src/Vigil/Core/ProcessFreezer.cs
- `App` --references--> `ProcessWatcher`  [EXTRACTED]
  src/Vigil/App.xaml.cs → src/Vigil/Core/ProcessWatcher.cs
- `App` --references--> `RecoveryJournal`  [EXTRACTED]
  src/Vigil/App.xaml.cs → src/Vigil/Core/RecoveryJournal.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Vigil Core Protection Subsystems** — src_vigil_core_powercontroller, src_vigil_core_thermalthrottler, src_vigil_core_wifiwatchdog, src_vigil_core_processfreezer, src_vigil_core_recoveryjournal [EXTRACTED 1.00]

## Communities (13 total, 0 thin omitted)

### Community 0 - "Session Coordination & Timer"
Cohesion: 0.13
Nodes (4): IRecoveryJournal, JournalFilePath, IPowerCfgRunner, PowerCfgRunner

### Community 1 - "C# Namespaces & Types"
Cohesion: 0.08
Nodes (12): Vigil.Core, Vigil.Core.Contracts, Vigil.Views, Vigil.Native, Vigil, Vigil.Tests, Vigil.Models, Auto-Pilot Process Trigger (+4 more)

### Community 2 - "Configuration & Auto-Pilot Models"
Cohesion: 0.07
Nodes (32): AutoPilotSettings, Enabled, WatchProcesses, BatterySettings, EcoQoSFloorPercent, SafeHibernateFloorPercent, FreezerSettings, Enabled (+24 more)

### Community 3 - "Win32 PInvoke & Native Interop"
Cohesion: 0.10
Nodes (11): IPowerController, PowerController, IsApplied, Name, Win32, ExecutionState, EsAwayModeRequired, EsContinuous (+3 more)

### Community 4 - "Application Entry & System Tray"
Cohesion: 0.13
Nodes (4): Vigil Application Specification, Application, App, FlyoutWindow

### Community 5 - "Fluent Bento UI Flyout"
Cohesion: 0.11
Nodes (28): Fluent Bento Grid UI Mockup, Plus Jakarta Sans & JetBrains Mono System, BtnAdd30, BtnCloseFlyout, BtnEndSession, ChkAutoWifi, DotStatus, ProgressBattery (+20 more)

### Community 6 - "Process Watcher & Freezer"
Cohesion: 0.09
Nodes (16): IProcessWatcher, CurrentTargetProcess, IsTargetProcessRunning, MonitoredProcesses, IProcessFreezer, SuspendedProcessIds, SuspendedProcessNames, ProcessFreezer (+8 more)

### Community 7 - "Session State Contracts"
Cohesion: 0.08
Nodes (24): ISessionCoordinator, ActiveTargetProcess, CurrentStatus, IsInfiniteSession, RemainingTime, SessionEndReason, AppExiting, BatteryCritical (+16 more)

### Community 8 - "Write-Ahead Recovery Journal"
Cohesion: 0.10
Nodes (18): RecoveryJournal, JournalFilePath, RecoveryData, AppliedSettings, ExpectedEndTimeUtc, FrozenProcessIds, MutedEndpoints, OriginalSettings (+10 more)

### Community 9 - "System Modifier Lifecycle"
Cohesion: 0.09
Nodes (17): ISystemModifier, IsApplied, Name, SessionCoordinator, ActiveTargetProcess, CurrentStatus, IsInfiniteSession, RemainingTime (+9 more)

### Community 10 - "Thermal Throttling Subsystem"
Cohesion: 0.16
Nodes (6): ThermalThrottler, CurrentCpuTempCelsius, CurrentMaxProcessorState, IsApplied, IsEmergencyThrottled, Name

### Community 11 - "Wi-Fi Watchdog & Networking"
Cohesion: 0.21
Nodes (6): WiFiWatchdog, CurrentConnectedSsid, IsApplied, IsInternetConnected, Name, PingLatencyMs

### Community 12 - "NuGet Packages & Build Dependencies"
Cohesion: 0.20
Nodes (8): Microsoft.NET.Test.Sdk (17.8.0), System.Management (8.0.0), xunit (2.6.2), xunit.runner.visualstudio (2.5.4), net11.0-windows, Microsoft.NET.Sdk, net11.0-windows, Microsoft.NET.Sdk

## Knowledge Gaps
- **119 isolated node(s):** `MonitoredProcesses`, `IsTargetProcessRunning`, `CurrentTargetProcess`, `Idle`, `Active` (+114 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 142 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `FlyoutWindow` connect `Application Entry & System Tray` to `C# Namespaces & Types`, `Fluent Bento UI Flyout`, `Process Watcher & Freezer`, `Session State Contracts`, `System Modifier Lifecycle`?**
  _High betweenness centrality (0.170) - this node is a cross-community bridge._
- **What connects `MonitoredProcesses`, `IsTargetProcessRunning`, `CurrentTargetProcess` to the rest of the system?**
  _119 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Session Coordination & Timer` be split into smaller, more focused modules?**
  _Cohesion score 0.12677304964539007 - nodes in this community are weakly interconnected._
- **Why does `VigilConfig` connect `Configuration & Auto-Pilot Models` to `Win32 PInvoke & Native Interop`, `Application Entry & System Tray`, `Process Watcher & Freezer`, `System Modifier Lifecycle`, `Thermal Throttling Subsystem`, `Wi-Fi Watchdog & Networking`?**
  _High betweenness centrality (0.155) - this node is a cross-community bridge._
- **Should `C# Namespaces & Types` be split into smaller, more focused modules?**
  _Cohesion score 0.08414634146341464 - nodes in this community are weakly interconnected._
- **Why does `App` connect `Application Entry & System Tray` to `Session Coordination & Timer`, `C# Namespaces & Types`, `Configuration & Auto-Pilot Models`, `Win32 PInvoke & Native Interop`, `Process Watcher & Freezer`, `Write-Ahead Recovery Journal`, `System Modifier Lifecycle`, `Thermal Throttling Subsystem`, `Wi-Fi Watchdog & Networking`?**
  _High betweenness centrality (0.152) - this node is a cross-community bridge._
- **Should `Configuration & Auto-Pilot Models` be split into smaller, more focused modules?**
  _Cohesion score 0.06825396825396825 - nodes in this community are weakly interconnected._