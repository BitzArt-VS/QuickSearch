namespace BitzArt.UI.Tweaks;

/// <summary>
/// Collapses any number of rapid <see cref="Trigger"/> calls into a single invocation
/// of the delegate provided at construction. The delegate fires once, after
/// <paramref name="delay"/> elapses without a further trigger. Calling
/// <see cref="Flush"/> cancels any pending delay and immediately invokes the delegate
/// instead — preserving the "don't lose work on dispose" contract.
/// </summary>
public sealed class Debouncer : IDisposable
{
    private readonly TimeSpan _delay;
    private readonly Action _action;
    private readonly Lock _lock = new();
    private CancellationTokenSource? _pendingTrigger;

    public Debouncer(TimeSpan delay, Action action)
    {
        _delay = delay;
        _action = action;
    }

    public void Trigger()
    {
        CancellationToken token;
        lock (_lock)
        {
            _pendingTrigger?.Cancel();
            _pendingTrigger?.Dispose();
            _pendingTrigger = new CancellationTokenSource();
            token = _pendingTrigger.Token;
        }

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_delay, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            _action();

            lock (_lock)
            {
                if (_pendingTrigger is not null && _pendingTrigger.Token == token)
                {
                    _pendingTrigger.Dispose();
                    _pendingTrigger = null;
                }
            }
        });
    }

    /// <summary>
    /// Cancels any pending delayed invocation and immediately invokes the action if
    /// one was outstanding. Use this in <see cref="IDisposable.Dispose"/> to ensure
    /// in-flight work is not silently dropped.
    /// </summary>
    public void Flush()
    {
        bool hasPending;
        lock (_lock)
        {
            hasPending = _pendingTrigger is not null;
            _pendingTrigger?.Cancel();
            _pendingTrigger?.Dispose();
            _pendingTrigger = null;
        }

        if (hasPending)
        {
            _action();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _pendingTrigger?.Cancel();
            _pendingTrigger?.Dispose();
            _pendingTrigger = null;
        }
    }
}
