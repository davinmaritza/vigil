using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Vigil.Native;

public interface IPowerCfgRunner
{
    Task<string?> GetActiveSchemeGuidAsync(CancellationToken ct = default);
    Task<int?> GetLidActionAsync(bool onBattery = false, CancellationToken ct = default);
    Task<bool> SetLidActionAsync(int action, bool onBattery = false, CancellationToken ct = default);

    Task<int?> GetMaxProcessorStateAsync(bool onBattery = false, CancellationToken ct = default);
    Task<bool> SetMaxProcessorStateAsync(int percentage, bool onBattery = false, CancellationToken ct = default);

    Task<bool> SetActiveSchemeAsync(CancellationToken ct = default);
}

public class PowerCfgRunner : IPowerCfgRunner
{
    // GUIDs for SUB_BUTTONS (4f971e89-eebd-4455-a8de-9e59040e7347) and LIDACTION (5ca83367-6e45-459f-a27b-476b1d01c936)
    private const string SubButtons = "4f971e89-eebd-4455-a8de-9e59040e7347";
    private const string LidActionSetting = "5ca83367-6e45-459f-a27b-476b1d01c936";

    // GUIDs for SUB_PROCESSOR (54533251-82be-4824-96c1-47b60b740d00) and PROCTHROTTLEMAX (bc5038f7-23e0-4960-96da-33abaf5935ec)
    private const string SubProcessor = "54533251-82be-4824-96c1-47b60b740d00";
    private const string ProcThrottleMax = "bc5038f7-23e0-4960-96da-33abaf5935ec";

    private static readonly Regex GuidRegex = new(
        @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
        RegexOptions.Compiled);

    public async Task<string?> GetActiveSchemeGuidAsync(CancellationToken ct = default)
    {
        // 1. In-process registry read (instantaneous and resilient)
        try
        {
            var regVal = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes",
                "ActivePowerScheme",
                null) as string;

            if (!string.IsNullOrWhiteSpace(regVal))
            {
                var match = GuidRegex.Match(regVal);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }
        catch
        {
            // Fallback to powercfg CLI
        }

        // 2. CLI query fallback
        var output = await RunCommandAsync("/getactivescheme", ct);
        if (!string.IsNullOrWhiteSpace(output))
        {
            var match = GuidRegex.Match(output);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    public async Task<int?> GetLidActionAsync(bool onBattery = false, CancellationToken ct = default)
    {
        var output = await RunCommandAsync($"/query SCHEME_CURRENT {SubButtons} {LidActionSetting}", ct);
        var parsed = ParsePowerSettingHex(output, onBattery);
        if (parsed.HasValue) return parsed;

        // Fallback: Registry direct lookup for Modern Standby / OEM hidden attributes
        var activeScheme = await GetActiveSchemeGuidAsync(ct);
        if (!string.IsNullOrWhiteSpace(activeScheme))
        {
            return GetSettingFromRegistry(activeScheme, SubButtons, LidActionSetting, onBattery);
        }

        return null;
    }

    public async Task<bool> SetLidActionAsync(int action, bool onBattery = false, CancellationToken ct = default)
    {
        var valueIndexCmd = onBattery ? "/setdcvalueindex" : "/setacvalueindex";
        var res = await RunCommandAsync($"{valueIndexCmd} SCHEME_CURRENT {SubButtons} {LidActionSetting} {action}", ct);
        await SetActiveSchemeAsync(ct);
        return res != null;
    }

    public async Task<int?> GetMaxProcessorStateAsync(bool onBattery = false, CancellationToken ct = default)
    {
        var output = await RunCommandAsync($"/query SCHEME_CURRENT {SubProcessor} {ProcThrottleMax}", ct);
        var parsed = ParsePowerSettingHex(output, onBattery);
        if (parsed.HasValue) return parsed;

        // Fallback: Registry direct lookup
        var activeScheme = await GetActiveSchemeGuidAsync(ct);
        if (!string.IsNullOrWhiteSpace(activeScheme))
        {
            return GetSettingFromRegistry(activeScheme, SubProcessor, ProcThrottleMax, onBattery);
        }

        return null;
    }

    public async Task<bool> SetMaxProcessorStateAsync(int percentage, bool onBattery = false, CancellationToken ct = default)
    {
        var clamped = Math.Clamp(percentage, 50, 100);
        var valueIndexCmd = onBattery ? "/setdcvalueindex" : "/setacvalueindex";
        var res = await RunCommandAsync($"{valueIndexCmd} SCHEME_CURRENT {SubProcessor} {ProcThrottleMax} {clamped}", ct);
        await SetActiveSchemeAsync(ct);
        return res != null;
    }

    public async Task<bool> SetActiveSchemeAsync(CancellationToken ct = default)
    {
        var res = await RunCommandAsync("/setactive SCHEME_CURRENT", ct);
        return res != null;
    }

    private static int? GetSettingFromRegistry(string schemeGuid, string subGroupGuid, string settingGuid, bool onBattery)
    {
        try
        {
            var valueName = onBattery ? "DCSettingIndex" : "ACSettingIndex";
            var keyPath = $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes\{schemeGuid}\{subGroupGuid}\{settingGuid}";
            var value = Registry.GetValue(keyPath, valueName, null);
            if (value is int intVal)
            {
                return intVal;
            }
        }
        catch
        {
            // Ignored, return null
        }
        return null;
    }

    private static async Task<string?> RunCommandAsync(string arguments, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            return stdout;
        }
        catch
        {
            return null;
        }
    }

    public static int? ParsePowerSettingHex(string? output, bool onBattery)
    {
        if (string.IsNullOrWhiteSpace(output)) return null;

        var targetKey = onBattery ? "Current DC Power Setting Index:" : "Current AC Power Setting Index:";
        using var reader = new StringReader(output);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Contains(targetKey, StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(':');
                if (parts.Length > 1)
                {
                    var hexStr = parts[1].Trim();
                    if (hexStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        hexStr = hexStr[2..];
                    }

                    if (int.TryParse(hexStr, System.Globalization.NumberStyles.HexNumber, null, out var val))
                    {
                        return val;
                    }
                }
            }
        }
        return null;
    }
}
