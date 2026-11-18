<p align="center">
  <img src="assets/logo-transparent.png" alt="Vigil: Windows Clamshell and Cooling Guard" width="112">
</p>

<h1 align="center">Vigil</h1>

<p align="center">
  <strong>Keep your Windows laptop awake. Cap thermals. Give the session an end time.</strong>
</p>

<p align="center">
  A Windows system tray app and CLI for timed clamshell sessions and thermal protection. Vigil lives in your taskbar notification area. Set how long your laptop stays awake with the lid closed, cap CPU temperatures to prevent heat buildup, and rely on write-ahead recovery journals so your original power settings always restore cleanly.
</p>

<p align="center">
  <a href="#install"><strong>Install</strong></a> ·
  <a href="#what-you-can-use-it-for"><strong>Use Cases</strong></a> ·
  <a href="#using-it"><strong>Using it</strong></a> ·
  <a href="#what-happens-when-the-lid-closes"><strong>Lid close</strong></a> ·
  <a href="#how-recovery-works"><strong>Recovery</strong></a> ·
  <a href="#cli-and-automation"><strong>CLI & Automation</strong></a> ·
  <a href="#acknowledgements"><strong>Acknowledgements</strong></a>
</p>

<p align="center">
  <a href="LICENSE"><img alt="MIT license" src="https://img.shields.io/badge/License-MIT-303336?style=flat-square"></a>
  <img alt="Windows 11 Build 26100+" src="https://img.shields.io/badge/Windows-11%20(24H2%2B)-303336?style=flat-square">
  <img alt=".NET 11.0" src="https://img.shields.io/badge/.NET-11.0-303336?style=flat-square">
  <img alt="Release v1.0.0" src="https://img.shields.io/badge/Release-v1.0.0-536D78?style=flat-square">
</p>

> **Use a stable, well-ventilated surface, not a closed bag.** Vigil is designed for overnight desktop workloads, local AI agents, and heavy builds. A running timer is not a substitute for airflow.

---

## What you can use it for

Vigil is built for any long-running task where you want to close your laptop lid, leave it on your desk, and prevent Windows from sleeping or overheating:

- **AI coding agents and background tooling**: Run Antigravity, Claude Code, Cursor, Aider, or long test suites overnight without keeping the lid open.
- **Local AI and model inference**: Keep Ollama, LM Studio, ComfyUI, vLLM, or PyTorch fine-tuning jobs running continuously.
- **3D rendering and simulation**: Leave Blender, Cinema 4D, Unreal Engine, or Maya rendering overnight.
- **Video exports and transcoding**: Let DaVinci Resolve, Adobe Premiere Pro, or Handbrake encodes finish with the screen turned off.
- **Large software builds**: Compile large Rust, C++, .NET, or Android Studio projects while away from your desk.
- **Overnight downloads and cloud sync**: Finish multi-gigabyte transfers without screen glare or accidental sleep interruptions.

---

## Install

Requires Windows 10 (Build 19041+) or Windows 11 (including 24H2 Build 26100+).

### Prebuilt release (recommended)

1. Download the latest self-contained archive from [Releases](https://github.com/davinmaritza/vigil/releases/latest).
2. Extract the folder anywhere on your computer.
3. Run `Vigil.exe`. The pure monochrome flyout will appear on your screen, and an icon will be added to your system tray.

To pin Vigil to your Windows taskbar or Start menu, right-click `Vigil.exe` and select **Pin to taskbar** or **Pin to Start**.

### Build from source

Requires the .NET 11 SDK on Windows:

```powershell
# Clone the repository
git clone https://github.com/davinmaritza/vigil.git
cd vigil

# Build and run tests
dotnet build src/Vigil/Vigil.csproj
dotnet test tests/Vigil.Tests/Vigil.Tests.csproj

# Publish standalone executable
dotnet publish src/Vigil/Vigil.csproj -c Release -r win-x64 --self-contained true -o dist
```

<details>
<summary><strong>What Vigil changes on your Windows system during a session</strong></summary>

Vigil modifies system power parameters temporarily while a session is active, then restores them when the session ends:

- **Lid-close action**: Sets AC and DC lid action (`LIDACTION` under power subgroup `SUB_BUTTONS`) to `0` (Do nothing) and asserts `SetThreadExecutionState` with execution flags. On modern Modern Standby laptops where OEM firmware hides this setting from `powercfg`, Vigil updates the index directly in the registry.
- **Processor maximum state cap**: Capped at `92%` (`PROCTHROTTLEMAX` under power subgroup `SUB_PROCESSOR`). This prevents Intel and AMD boost algorithms from applying peak voltages, keeping core temperatures in the 60°C to 70°C range under sustained load.
- **Display power off**: Calls `SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, 2)` to turn off internal displays, eliminating backlight heat and saving power.
- **Write-ahead recovery journal**: Saved atomically to `%LOCALAPPDATA%\Vigil\recovery.json` before any settings are modified.
- **Watchdog scheduled task**: Schedules an out-of-process task in Windows Task Scheduler set for one minute past session expiration. If Vigil or the operating system crashes, the scheduled task runs `Vigil.exe --recover` to restore your original power scheme.

</details>

---

## Using it

1. **Start.** Launch Vigil or click the icon in your system tray. Click a preset chip (`30m`, `1h`, `2h`, `4h`, or `∞`) to start immediately.
2. **Add time.** Click `+30 Menit` anytime during an active session to extend the remaining duration.
3. **Move and minimize.** Drag the window anywhere on your screen by clicking and holding any open space. Click the minimize button (`—`) in the header to minimize the flyout, or click the close button (`✕`) to hide it cleanly into the system tray.
4. **End early.** Click `Akhiri` on the dashboard, or right-click the system tray icon and choose **End Active Session**. Original power policies and process states are restored instantly.
5. **Auto-Pilot.** In `config.json`, add process names you want Vigil to watch (such as `blender.exe` or `ollama.exe`). When the monitored process launches, Vigil arms itself automatically; when the process exits, Vigil cleanly restores your machine.

---

## What happens when the lid closes

Closing a laptop lid on Windows normally triggers Modern Standby, which suspends background user processes and pauses active terminals.

If you manually set your lid action to "Do nothing" in Windows Control Panel, modern processors will boost to maximum frequency. In a closed chassis where cooling exhausts are partially obstructed by the hinge, CPU temperatures can rapidly climb above 90°C, leading to thermal throttling or sudden safety shutoffs.

Vigil resolves both problems:

1. **Keeps tasks running**: Prevents sleep mode while the session is active.
2. **Suppresses thermal spikes**: The 92 percent CPU cap restricts peak voltage requests, dropping sustained temperatures by 20°C to 25°C with less than 5 percent impact on multi-core compute throughput.
3. **Blacks out internal screens**: Eliminates display heat from radiating into the closed keyboard deck.
4. **Maintains network connectivity**: Pings the local gateway every 30 seconds and automatically reconnects Wi-Fi if Windows drops the adapter while headless.

---

## How recovery works

Vigil uses a two-tier write-ahead crash journaling design:

1. Before modifying any power settings, Vigil queries and snapshots your active power scheme GUID, original lid action values, and maximum processor states.
2. The snapshot is written atomically to `%LOCALAPPDATA%\Vigil\recovery.json` using temporary staging and `File.Replace`.
3. A scheduled task backstop is registered with Windows Task Scheduler set to run at `sessionEnd + 1 minute`.
4. Only after the journal is committed does Vigil modify power policies and suspend background apps.

When a session ends normally, original settings are restored, suspended processes are resumed, the journal file is deleted, and the watchdog task is cancelled.

If your laptop loses power, restarts, or crashes during a session, Vigil detects the active journal on next boot (or via the Task Scheduler backstop) and restores your original power configuration automatically.

---

## CLI and automation

Vigil includes a full command-line interface, allowing background scripts, dev containers, and AI coding agents to control sessions programmatically.

```powershell
# Show command-line help
Vigil.exe --help

# Query current session, temperature, and Wi-Fi state as JSON
Vigil.exe --session status --json

# Start a session for 7200 seconds (2 hours)
Vigil.exe --session start 7200

# Extend active session by 1800 seconds (30 minutes)
Vigil.exe --session extend 1800

# End active session and restore power settings
Vigil.exe --session end

# Recover settings from disk journal after a crash
Vigil.exe --recover
```

### Scripting with AI coding agents

You can chain Vigil with local AI coding agents or long build pipelines in terminal scripts:

```powershell
# Arm Vigil for 2 hours, run Claude Code, and restore power settings on completion
Vigil.exe --session start 7200; claude; Vigil.exe --session end

# Run Antigravity with clamshell protection
Vigil.exe --session start 14400; antigravity run; Vigil.exe --session end
```

### JSON telemetry output

Calling `Vigil.exe --session status --json` returns structured telemetry:

```json
{
  "status": "Active",
  "remaining_seconds": 5420,
  "target_process": "claude.exe",
  "cpu_temp_celsius": 61.0,
  "cpu_max_state": 92,
  "wifi_ssid": "Studio-5GHz"
}
```

---

## Optional extras

<details>
<summary><strong>Background process freezer</strong></summary>

During heavy compute jobs, background applications consume memory bandwidth and CPU cycles. Vigil can automatically suspend background processes (such as `slack.exe`, `discord.exe`, or `spotify.exe`) using the native NT API `NtSuspendProcess`, and resume them cleanly with `NtResumeProcess` when the session completes.

System processes and Vigil itself are protected by hardcoded safety filters and bypass lists.

</details>

<details>
<summary><strong>Wi-Fi watchdog</strong></summary>

Windows occasionally disables network adapters when operating headless with the display powered down. Vigil pings your default gateway periodically. If packets drop repeatedly, Vigil invokes `netsh wlan connect` using your active profile to re-establish the connection.

</details>

<details>
<summary><strong>Configuration file</strong></summary>

Configuration settings are stored in `%LOCALAPPDATA%\Vigil\config.json`. You can configure default session durations, CPU throttling percentages, monitored process lists, and background freezer targets.

</details>

---

## Development

Run the automated test suite from the repository root:

```powershell
dotnet test tests/Vigil.Tests/Vigil.Tests.csproj
```

The test suite validates power policy parsing, registry fallbacks, recovery journal atomic write/read cycles, corrupted journal handling, and process freezer safety filters.

---

## Star history

<a href="https://www.star-history.com/?repos=davinmaritza%2Fvigil&amp;type=date&amp;legend=top-left">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/svg?repos=davinmaritza/vigil&amp;type=date&amp;theme=dark&amp;legend=top-left" />
    <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/svg?repos=davinmaritza/vigil&amp;type=date&amp;legend=top-left" />
    <img alt="Star history for davinmaritza/vigil" src="https://api.star-history.com/svg?repos=davinmaritza/vigil&amp;type=date&amp;legend=top-left" />
  </picture>
</a>

---

## Acknowledgements

- Inspired by [Insomnia](https://github.com/krishhgg/Insomnia) by [krishhgg](https://github.com/krishhgg) for the core concept of laptop clamshell awake utilities.

---

## License

[MIT](LICENSE)
