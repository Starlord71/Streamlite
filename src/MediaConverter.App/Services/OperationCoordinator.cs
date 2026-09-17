using System;
using System.Threading;

namespace MediaConverter.App.Services;

/// <summary>
/// Coordinates the long-running operations started from the Razor UI. It tracks how many operations
/// are in flight so the shell can disable global controls (tabs and the language selector) and so a
/// component can refuse to start a second operation while another one is still running. The type
/// has no UI or WPF dependency and is safe to unit test.
/// </summary>
public sealed class OperationCoordinator
{
    private int _activeCount;

    /// <summary>
    /// Raised whenever the busy state changes, so the shell can re-render its global controls.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Gets a value indicating whether at least one operation is currently in progress.
    /// </summary>
    public bool IsBusy => Volatile.Read(ref _activeCount) > 0;

    /// <summary>
    /// Marks the start of an operation and returns a scope that marks its end when disposed. Dispose
    /// the returned scope (for example with a <c>using</c> or a <c>finally</c> block) so the busy
    /// state is always released, even when the operation is cancelled or throws.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that releases the busy state when disposed.</returns>
    public IDisposable Begin()
    {
        Interlocked.Increment(ref _activeCount);
        Changed?.Invoke(this, EventArgs.Empty);
        return new Scope(this);
    }

    private void End()
    {
        var remaining = Interlocked.Decrement(ref _activeCount);

        // Guard against unbalanced calls: the counter must never go negative, or IsBusy would stay
        // false while a stale increment keeps the shell locked.
        if (remaining < 0)
        {
            Interlocked.Exchange(ref _activeCount, 0);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class Scope : IDisposable
    {
        private OperationCoordinator? _owner;

        public Scope(OperationCoordinator owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            // Exchange makes Dispose idempotent, so a double dispose only releases the state once.
            Interlocked.Exchange(ref _owner, null)?.End();
        }
    }
}
