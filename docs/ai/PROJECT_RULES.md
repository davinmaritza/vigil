# Project Rules: Vigil (Windows Clamshell & Cooling Guard)

These rules are vendor-neutral and apply to all software engineers, architects, and AI assistants working on **Vigil**.

---

## 1. Core Operating Principles

1. **Repository as Implementation Truth**: The repository source code (`src/Vigil/`) is the sole authority for what is currently implemented. Notion and documentation represent knowledge, architectural intent, decisions, and task tracking.
2. **Notion as Knowledge Authority**: Notion is the source of truth for architectural decision records (ADRs), progress, memory, and task states. Synchronize Notion after meaningful code or architecture changes.
3. **Inspect Before Changing**: Read existing code, contracts (`src/Vigil/Core/Contracts/`), and configuration before making modifications.
4. **Safety & Zero Destructive Actions**: Never modify Windows power policies without recording the original values in the write-ahead recovery journal. Always maintain the Task Scheduler backstop.
5. **No AI Slop / High-End Design**:
   - UI must follow the Bento Widget specification (`docs/mockups/vigil-ui-mockup.html`).
   - Fonts: `Plus Jakarta Sans` for UI copy; `JetBrains Mono` for tabular metrics and telemetry.
   - No neon glows, no gradient drop-shadow buttons, no conversational filler or decorative emojis in headers.

---

## 2. Standard Workflow

Always follow this sequence:
```text
Detect Runtime -> Read Context -> Inspect Repository -> Plan -> Implement -> Validate -> Update Knowledge -> Sync Notion -> Update AI Context
```

1. **Read Context**: Check `docs/ai/AI_CONTEXT.md` and Notion `18. AI Context` before starting tasks.
2. **Review Decisions**: Consult `05. Project Decisions` before making architectural alterations.
3. **Implement**: Keep code simple (KISS), avoid premature abstractions (YAGNI), and separate concerns (SOLID).
4. **Validate**:
   - Rebuild: `dotnet build src/Vigil/Vigil.csproj`
   - Test: `dotnet test tests/Vigil.Tests/Vigil.Tests.csproj`
5. **Sync**:
   - If Notion is available, push updates to corresponding sections.
   - If Notion is unavailable, log pending changes in `docs/ai/PENDING_NOTION_SYNC.md`.

---

## 3. Architecture & Implementation Guidelines

- **Target Framework**: .NET 11.0 Windows (`net11.0-windows`), C# 12, WPF.
- **P/Invoke Safety**:
  - Encapsulate native Win32/NT calls in `src/Vigil/Native/Win32.cs`.
  - Use `SafeHandle` or ensure deterministic cleanup for native handles.
  - Wrap process freezing in `NtSuspendProcess` and `NtResumeProcess` with valid exception handling.
- **Power Configuration**:
  - Always query current scheme with `powercfg /getactivescheme`.
  - Modify lid action using `powercfg /setacvalueindex` and `powercfg /setdcvalueindex` for GUID `SUB_BUTTONS` and `LIDACTION`.
  - Throttle processor via GUID `SUB_PROCESSOR` and `PROCTHROTTLEMAX`.
- **Write-Ahead Crash Journaling**:
  - State changes must write atomically to `%LOCALAPPDATA%\Vigil\recovery.json` using `File.Replace`.
  - Schedule watchdog task via `schtasks.exe` set to `expectedEnd + 1 minute`.
- **Sensors & Networking**:
  - WMI temperature polling via `MSAcpi_ThermalZoneTemperature` or fallback CPU load sampling.
  - Wi-Fi watchdog via ICMP ping (`8.8.8.8` / default gateway) and `netsh wlan connect`.

---

## 4. Security & Privacy Rules

- **Never Commit Secrets**: Do not store Wi-Fi passwords, private keys, tokens, or environment credentials in the repository or Notion.
- **Non-Elevated Operation Where Possible**: Ensure core CLI functions and UI work under standard user permissions, escalating only when modifying system-wide power schemes.
