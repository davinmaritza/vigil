<p align="center">
  <img src="assets/vigil-fused-c2-light.jpg" alt="Vigil" width="150" height="150" style="border-radius: 32px; border: 1px solid #E2E8F0;" />
</p>

# Vigil

Vigil is a Windows system tray app designed to keep your laptop running smoothly with the lid closed. It features an interactive Bento Grid dashboard for one-click sessions, provides an optional CLI for automated scripting, caps CPU frequencies to stop heat buildup, and writes a recovery log so settings revert when you finish.

## What you can use it for

Vigil works for any long-running task where you want to close your laptop lid, leave it on your desk, and prevent Windows from sleeping or overheating:

- **AI coding agents and developer tools**: Antigravity, Claude Code, Cursor, Aider, Roo Code, or long test suites.
- **Local AI and model inference**: Ollama, LM Studio, ComfyUI, vLLM, or PyTorch fine-tuning.
- **3D rendering and simulation**: Blender, Cinema 4D, Unreal Engine, or Maya renders left overnight.
- **Video exports and transcoding**: DaVinci Resolve, Adobe Premiere Pro, or Handbrake encodes.
- **Large software builds**: Long C++, Rust, or Android Studio compilations.
- **Overnight downloads and cloud sync**: Large game updates or file syncs without keeping the screen lit.

## The problem

Closing a laptop lid on Windows triggers Modern Standby, which suspends background processes and stops active jobs.

If you set the lid action to "Do nothing" in Windows power settings, modern processors continue using full Turbo Boost clocks. Inside a closed chassis with restricted airflow, temperatures quickly climb past 90°C, causing hard thermal throttling or emergency shutdowns.

Vigil handles both issues:
- Keeps the laptop awake for a chosen duration or until a monitored application exits.
- Caps maximum processor state at 92 percent, stopping Turbo Boost voltage spikes and keeping CPU temperatures in the 60°C to 70°C range.
- Shuts off internal displays immediately to save power and stop heat from radiating into the keyboard.
- Maintains Wi-Fi connectivity and pauses background apps to free up system memory and cores.
- Reverts all settings back to normal when the timer expires or if the machine suddenly restarts.

> [!WARNING]
> Keep the laptop on a flat desk or cooling stand during clamshell runs. Do not run it inside a closed bag.

## Graphical interface (GUI)

Vigil is built primarily as a lightweight system tray application. You do not need to use the command line:

1. Click the Vigil icon in your Windows notification area (next to the system clock).
2. The **Bento Grid Flyout** opens:
   - **Preset pills**: Click `30m`, `1h`, `2h`, `4h`, `8h`, or `∞` to start instantly.
   - **Extend session**: Click `+30m` anytime to add more time.
   - **Live hardware telemetry**: See live CPU temperature in real time, current processor load, active cap (92%), Wi-Fi ping latency, and battery runtime.
   - **Switches**: Toggle clamshell mode, screen blackout, Wi-Fi auto-reconnect, and "Run on Windows Startup" directly with one click.
   - **End session**: Click `End Session` to immediately restore normal Windows sleep behavior.

An interactive preview of the dashboard design is available at [docs/mockups/vigil-ui-mockup.html](docs/mockups/vigil-ui-mockup.html).

## Running via CLI and automation

If you prefer terminal workflows or want AI agents to control Vigil programmatically, use the built-in CLI commands:

```powershell
# Show options
Vigil.exe --help

# Return JSON telemetry for agents or scripts
Vigil.exe --session status --json

# Start a 2-hour session (in seconds)
Vigil.exe --session start 7200

# Add 30 minutes to current session
Vigil.exe --session extend 1800

# End session and restore power settings
Vigil.exe --session end

# Recover settings from disk journal
Vigil.exe --recover
```

### Scripting with AI agents or builds
You can chain commands in terminal scripts:
```powershell
# Arm Vigil for 2 hours, run Claude Code, and restore settings on exit
Vigil.exe --session start 7200; claude; Vigil.exe --session end
```

### Automatic process watching
You can add target process names (such as `blender.exe`, `ollama.exe`, or `python.exe`) to `config.json`. When the monitored process launches, Vigil arms automatically; when it finishes, Vigil reverts your original power state.

## Core features

### Clamshell mode
Changes the lid action setting to 0 and asserts `SetThreadExecutionState` with continuous execution flags. If OEM firmware hides the power configuration entry, Vigil updates the value directly in the Windows Registry.

### Thermal capping
Restricting the processor cap to 92 percent stops modern Intel and AMD chips from requesting maximum boost voltage. This keeps core temperatures around 60°C to 70°C under sustained workloads.

### Screen blackout
Calls `SC_MONITORPOWER` to shut off internal displays when a session begins, reducing panel wear and power consumption.

### Network watchdog
Pings the local gateway periodically. If Windows drops the adapter while headless, Vigil runs `netsh wlan connect` to reconnect.

### Background process pause
Uses `NtSuspendProcess` to pause selected background programs (such as chat clients or music players) during heavy computation, and resumes them when the session ends.

### Process monitoring
Can monitor a target binary like `ollama.exe` or `python.exe`. When the process starts, Vigil activates; when it finishes, Vigil reverts all settings.

## Recovery design

Vigil writes a disk journal before applying modifications:

1. Takes a snapshot of current power settings.
2. Writes the snapshot atomically to `%LOCALAPPDATA%\Vigil\recovery.json`.
3. Creates a scheduled task backstop set to trigger one minute after session expiration.
4. Applies the power policies and process suspensions.

On normal exit, settings restore and the log is removed. On a sudden restart or crash, Vigil reads the journal on next launch and reverts the machine to its previous state.

## Installation and build

### Prebuilt binary
Run the compiled executable directly from the repository:
```powershell
& "dist\Vigil.exe"
```

### Build from source
Requires .NET 11 SDK on Windows:

```powershell
dotnet build src/Vigil/Vigil.csproj
dotnet test tests/Vigil.Tests/Vigil.Tests.csproj
dotnet publish src/Vigil/Vigil.csproj -c Release -r win-x64 --self-contained true -o dist
```

## Command line usage

Vigil supports direct terminal calls for scripting and automation:

```powershell
# Show help
Vigil.exe --help

# Return current status as JSON
Vigil.exe --session status --json

# Start a session for 7200 seconds (2 hours)
Vigil.exe --session start 7200

# Add 1800 seconds to an active session
Vigil.exe --session extend 1800

# Stop the active session and restore settings
Vigil.exe --session end

# Restore previous settings from disk journal
Vigil.exe --recover
```

### JSON output sample

```json
{
  "status": "Active",
  "remaining_seconds": 7140,
  "target_process": "ollama.exe",
  "cpu_temp_celsius": 52.0,
  "cpu_max_state": 92,
  "wifi_ssid": "Studio-5GHz"
}
```

## Testing

Run the test suite:
```powershell
dotnet test tests/Vigil.Tests/Vigil.Tests.csproj
```
The suite verifies power parsing, registry fallbacks, journal write/read cycles, corruption tolerance, and process filter safety.

## License

MIT License. See LICENSE for details.
