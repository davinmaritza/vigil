namespace Vigil.Core.Contracts;

public interface IBatteryMonitor
{
    bool IsOnBattery { get; }
    int BatteryLifePercent { get; }
    bool HasBattery { get; }
    bool IsBatteryCritical(int thresholdPercent);
}
