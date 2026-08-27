using Microsoft.Extensions.Primitives;

namespace AutomotiveInfo.Caching;

/// <summary>
/// One shared invalidation signal for all cached news lists (one cache entry per culture).
/// Consumers attach <see cref="CreateChangeToken"/> to their cache entries; publishing an
/// article triggers <see cref="Invalidate"/>, which expires every attached entry at once —
/// without the invalidator needing to know the per-culture key set.
/// </summary>
public sealed class NewsCacheSignal : IDisposable
{
    private readonly object _lock = new();
    private CancellationTokenSource _cts = new();

    public IChangeToken CreateChangeToken()
    {
        lock (_lock)
        {
            return new CancellationChangeToken(_cts.Token);
        }
    }

    public void Invalidate()
    {
        CancellationTokenSource previous;
        lock (_lock)
        {
            previous = _cts;
            _cts = new CancellationTokenSource();
        }

        previous.Cancel();
        previous.Dispose();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _cts.Dispose();
        }
    }
}
