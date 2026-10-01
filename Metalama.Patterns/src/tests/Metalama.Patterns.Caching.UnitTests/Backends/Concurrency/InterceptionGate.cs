// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Diagnostics;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// A single-use gate armed on an <see cref="InterceptingMemoryCache"/>. The gate blocks the thread that performs the
/// matching call until the test calls <see cref="Release"/>, or throws an exception in that thread when the gate is a
/// fault.
/// </summary>
/// <remarks>
/// The gate matches a call when the operation is the same, the key satisfies the key predicate, and the optional
/// condition, evaluated on the calling thread, returns <see langword="true"/>. The gate trips on the matching call that
/// follows the number of matching calls given as the skip count. Only that call blocks. The gate waits on a monitor, so
/// it owns no disposable resource.
/// </remarks>
internal sealed class InterceptionGate
{
    private readonly object _sync = new();
    private readonly InterceptedOperation _operation;
    private readonly Func<string, bool> _keyPredicate;
    private readonly Func<bool>? _condition;
    private readonly Func<Exception>? _createException;

    private int _remainingSkips;
    private bool _isTripped;
    private bool _isReached;
    private bool _isReleased;

    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptionGate"/> class.
    /// </summary>
    /// <param name="operation">The intercepted operation that the gate matches.</param>
    /// <param name="keyPredicate">A predicate that the full cache key must satisfy.</param>
    /// <param name="skip">The number of matching calls that pass before the gate trips.</param>
    /// <param name="condition">A predicate evaluated on the calling thread, or <see langword="null"/>.</param>
    /// <param name="createException">
    /// A function that creates the exception that the gate throws when it trips, or <see langword="null"/> for a gate
    /// that blocks.
    /// </param>
    public InterceptionGate(
        InterceptedOperation operation,
        Func<string, bool> keyPredicate,
        int skip,
        Func<bool>? condition,
        Func<Exception>? createException )
    {
        this._operation = operation;
        this._keyPredicate = keyPredicate;
        this._remainingSkips = skip;
        this._condition = condition;
        this._createException = createException;
    }

    /// <summary>
    /// Gets the full cache key of the call that tripped the gate, or <see langword="null"/> when the gate has not tripped.
    /// </summary>
    public string? TrippedKey { get; private set; }

    /// <summary>
    /// Gets the name of the thread that tripped the gate, or <see langword="null"/> when the gate has not tripped.
    /// </summary>
    public string? TrippedThreadName { get; private set; }

    /// <summary>
    /// Gets the value that the tripping call read. It is set only for a gate on
    /// <see cref="InterceptedOperation.TryGetValueAfter"/>.
    /// </summary>
    public object? ObservedValue { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a call has tripped the gate.
    /// </summary>
    public bool HasTripped
    {
        get
        {
            lock ( this._sync )
            {
                return this._isTripped;
            }
        }
    }

    /// <summary>
    /// Called by the <see cref="InterceptingMemoryCache"/> for each intercepted call. Blocks the calling thread, or
    /// throws, when the call trips the gate.
    /// </summary>
    internal void OnIntercepted( InterceptedOperation operation, string key, object? observedValue )
    {
        if ( operation != this._operation || !this._keyPredicate( key ) )
        {
            return;
        }

        if ( this._condition != null && !this._condition() )
        {
            return;
        }

        lock ( this._sync )
        {
            if ( this._isTripped )
            {
                return;
            }

            if ( this._remainingSkips > 0 )
            {
                this._remainingSkips--;

                return;
            }

            this._isTripped = true;
            this.TrippedKey = key;
            this.TrippedThreadName = Thread.CurrentThread.Name;
            this.ObservedValue = observedValue;
            this._isReached = true;
            Monitor.PulseAll( this._sync );

            if ( this._createException != null )
            {
                throw this._createException();
            }

            while ( !this._isReleased )
            {
                Monitor.Wait( this._sync );
            }
        }
    }

    /// <summary>
    /// Waits until a call has tripped the gate. The timeout only detects a failure of the test.
    /// </summary>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <returns><see langword="true"/> when a call has tripped the gate, otherwise <see langword="false"/>.</returns>
    public bool WaitUntilReached( TimeSpan timeout )
    {
        var stopwatch = Stopwatch.StartNew();

        lock ( this._sync )
        {
            while ( !this._isReached )
            {
                var remaining = timeout - stopwatch.Elapsed;

                if ( remaining <= TimeSpan.Zero )
                {
                    return false;
                }

                Monitor.Wait( this._sync, remaining );
            }

            return true;
        }
    }

    /// <summary>
    /// Releases the thread that is blocked at the gate. When no thread has tripped the gate yet, the gate no longer
    /// blocks the call that trips it.
    /// </summary>
    public void Release()
    {
        lock ( this._sync )
        {
            this._isReleased = true;
            Monitor.PulseAll( this._sync );
        }
    }
}
