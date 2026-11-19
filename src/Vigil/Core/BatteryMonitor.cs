using Vigil.Core.Contracts;
using Vigil.Native;

namespace Vigil.Core;

public class BatteryMonitor : IBatteryMonitor
{
    public bool IsOnBattery
    {
        get
        {
            if (Win32.GetSystemPowerStatus(out var status))
            {
                return status.AcLineStatus == 0;
            }
            return false;
        }
    }

    public int BatteryLifePercent
    {
        get
        {
            if (Win32.GetSystemPowerStatus(out var status))
            {
                return status.BatteryLifePercent != 255 ? status.BatteryLifePercent : 100;
            }
            return 100;
        }
    }

    public bool HasBattery
    {
        get
        {
            if (Win32.GetSystemPowerStatus(out var status))
            {
                return (status.BatteryFlag & 128) == 0 && status.BatteryFlag != 255;
            }
            return false;
        }
    }

    public bool IsBatteryCritical(int thresholdPercent)
    {
        if (Win32.GetSystemPowerStatus(out var status))
        {
            // Must be on battery (AcLineStatus == 0) and reported valid percentage <= threshold
            if (status.AcLineStatus == 0 && status.BatteryLifePercent != 255)
            {
                return status.BatteryLifePercent <= thresholdPercent;
            }
        }
        return false;
    }
}
