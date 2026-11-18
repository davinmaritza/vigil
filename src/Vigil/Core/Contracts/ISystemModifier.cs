namespace Vigil.Core.Contracts;

public interface ISystemModifier : IAsyncDisposable, IDisposable
{
    string Name { get; }
    bool IsApplied { get; }
    Task ApplyAsync(CancellationToken ct = default);
    Task RevertAsync(CancellationToken ct = default);
}
