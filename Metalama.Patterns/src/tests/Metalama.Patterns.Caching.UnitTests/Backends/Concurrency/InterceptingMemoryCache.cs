// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// An <see cref="IMemoryCache"/> that forwards every call to another <see cref="IMemoryCache"/> and lets a test block
/// the calling thread at a chosen operation on a chosen key, throw an exception there, or defer the post-eviction
/// callbacks of chosen keys. A test uses it to force an interleaving of
/// <see cref="Metalama.Patterns.Caching.Backends.MemoryCachingBackend"/> without a synchronization point in the product.
/// </summary>
/// <remarks>
/// <para>
/// The backend prefixes its keys with <c>MemoryCachingBackend:&lt;id&gt;:item:</c> and
/// <c>MemoryCachingBackend:&lt;id&gt;:dependency:</c>, where the identifier is private to the instance. The predicates
/// returned by <see cref="ItemKey"/>, <see cref="DependencyKey"/> and <see cref="AnyItemKey"/> therefore match a
/// suffix.
/// </para>
/// <para>
/// A gate counts the matching calls from the moment it is armed, so a test arms a gate after its preparatory calls. When
/// several threads perform the same operation on the same key, the test names its threads and passes the predicate
/// returned by <see cref="OnThread"/>.
/// </para>
/// <para>
/// A gate encodes the order in which the backend calls the cache. A change of that order in the product makes the gate
/// trip at another place, or not at all, and the test then fails when it waits for the gate.
/// </para>
/// </remarks>
internal sealed partial class InterceptingMemoryCache : IClearableMemoryCache
{
    private readonly IMemoryCache _inner;
    private readonly object _sync = new();
    private readonly List<InterceptionGate> _gates = new();
    private readonly List<Func<string, bool>> _deferredCallbackKeyPredicates = new();
    private readonly List<CapturedEvictionCallback> _capturedCallbacks = new();
    private readonly ConcurrentQueue<InterceptedCall> _calls = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptingMemoryCache"/> class.
    /// </summary>
    /// <param name="inner">The cache to which every call is forwarded.</param>
    public InterceptingMemoryCache( IMemoryCache inner )
    {
        this._inner = inner;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptingMemoryCache"/> class that forwards its calls to a new
    /// <see cref="MemoryCache"/> with default options.
    /// </summary>
    public InterceptingMemoryCache() : this( new MemoryCache( new MemoryCacheOptions() ) ) { }

    /// <summary>
    /// Returns a predicate that matches the cache key under which the backend stores the item <paramref name="key"/>.
    /// </summary>
    public static Func<string, bool> ItemKey( string key ) => k => k.EndsWith( ":item:" + key, StringComparison.Ordinal );

    /// <summary>
    /// Returns a predicate that matches the cache key under which the backend stores the dependency set of
    /// <paramref name="key"/>.
    /// </summary>
    public static Func<string, bool> DependencyKey( string key ) => k => k.EndsWith( ":dependency:" + key, StringComparison.Ordinal );

    /// <summary>
    /// Returns a predicate that matches the cache key of any item.
    /// </summary>
    public static Func<string, bool> AnyItemKey() => k => k.Contains( ":item:", StringComparison.Ordinal );

    /// <summary>
    /// Returns a predicate that returns <see langword="true"/> when it is evaluated on the thread named
    /// <paramref name="threadName"/>.
    /// </summary>
    public static Func<bool> OnThread( string threadName ) => () => Thread.CurrentThread.Name == threadName;

    /// <summary>
    /// Gets the calls intercepted so far, in the order in which they were intercepted.
    /// </summary>
    public IReadOnlyList<InterceptedCall> Calls => this._calls.ToArray();

    /// <summary>
    /// Arms a gate that blocks the matching call that follows <paramref name="skip"/> matching calls.
    /// </summary>
    /// <param name="operation">The intercepted operation.</param>
    /// <param name="keyPredicate">A predicate that the full cache key must satisfy.</param>
    /// <param name="skip">The number of matching calls that pass before the gate trips.</param>
    /// <param name="condition">A predicate evaluated on the calling thread, such as the one returned by <see cref="OnThread"/>.</param>
    /// <returns>The gate.</returns>
    public InterceptionGate Arm(
        InterceptedOperation operation,
        Func<string, bool> keyPredicate,
        int skip = 0,
        Func<bool>? condition = null )
        => this.AddGate( new InterceptionGate( operation, keyPredicate, skip, condition, null ) );

    /// <summary>
    /// Arms a gate that throws an exception in the matching call that follows <paramref name="skip"/> matching calls.
    /// </summary>
    /// <param name="operation">The intercepted operation.</param>
    /// <param name="keyPredicate">A predicate that the full cache key must satisfy.</param>
    /// <param name="createException">A function that creates the exception to throw.</param>
    /// <param name="skip">The number of matching calls that pass before the gate trips.</param>
    /// <param name="condition">A predicate evaluated on the calling thread.</param>
    /// <returns>The gate, which records the tripping call.</returns>
    public InterceptionGate ArmFault(
        InterceptedOperation operation,
        Func<string, bool> keyPredicate,
        Func<Exception> createException,
        int skip = 0,
        Func<bool>? condition = null )
        => this.AddGate( new InterceptionGate( operation, keyPredicate, skip, condition, createException ) );

    private InterceptionGate AddGate( InterceptionGate gate )
    {
        lock ( this._sync )
        {
            this._gates.Add( gate );
        }

        return gate;
    }

    /// <summary>
    /// Counts the intercepted calls of an operation whose key satisfies a predicate.
    /// </summary>
    public int GetCallCount( InterceptedOperation operation, Func<string, bool> keyPredicate )
        => this._calls.Count( c => c.Operation == operation && keyPredicate( c.Key ) );

    /// <summary>
    /// Defers the post-eviction callbacks of the entries whose key satisfies <paramref name="keyPredicate"/> and that are
    /// stored after this call. A deferred callback runs only when the test calls <see cref="RunCapturedCallbacks"/>.
    /// </summary>
    /// <remarks>
    /// The callbacks are replaced before the entry is stored.
    /// <see cref="PostEvictionCallbackRegistration.EvictionCallback"/> is read when the eviction happens, so this works
    /// both with <see cref="MemoryCache"/>, which invokes the callbacks on the thread pool, and with
    /// <see cref="Metalama.Patterns.Caching.TestHelpers.FakeMemoryCache"/>, which invokes them on the evicting thread.
    /// </remarks>
    public void DeferEvictionCallbacks( Func<string, bool> keyPredicate )
    {
        lock ( this._sync )
        {
            this._deferredCallbackKeyPredicates.Add( keyPredicate );
        }
    }

    /// <summary>
    /// Waits until <paramref name="count"/> post-eviction callbacks have been captured in total. The timeout only
    /// detects a failure of the test.
    /// </summary>
    public bool WaitUntilCallbacksCaptured( int count, TimeSpan timeout )
    {
        var stopwatch = Stopwatch.StartNew();

        lock ( this._sync )
        {
            while ( this._capturedCallbacks.Count < count )
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
    /// Gets the key and the eviction reason of each captured post-eviction callback that has not run yet.
    /// </summary>
    public IReadOnlyList<(string Key, EvictionReason Reason)> PendingCapturedCallbacks
    {
        get
        {
            lock ( this._sync )
            {
                return this._capturedCallbacks.Where( c => !c.HasRun ).Select( c => (c.Key, c.Reason) ).ToList();
            }
        }
    }

    /// <summary>
    /// Runs, on the current thread, the captured post-eviction callbacks that have not run yet.
    /// </summary>
    /// <returns>The number of callbacks that ran.</returns>
    public int RunCapturedCallbacks()
    {
        List<CapturedEvictionCallback> callbacks;

        lock ( this._sync )
        {
            callbacks = this._capturedCallbacks.Where( c => !c.HasRun ).ToList();

            foreach ( var callback in callbacks )
            {
                callback.HasRun = true;
            }
        }

        foreach ( var callback in callbacks )
        {
            callback.Run();
        }

        return callbacks.Count;
    }

    /// <summary>
    /// Records an intercepted call and lets every armed gate inspect it.
    /// </summary>
    private void Intercept( InterceptedOperation operation, object key, object? observedValue = null )
    {
        if ( key is not string stringKey )
        {
            return;
        }

        this._calls.Enqueue( new InterceptedCall( operation, stringKey, Thread.CurrentThread.Name ) );

        InterceptionGate[] gates;

        lock ( this._sync )
        {
            gates = this._gates.ToArray();
        }

        foreach ( var gate in gates )
        {
            gate.OnIntercepted( operation, stringKey, observedValue );
        }
    }

    /// <summary>
    /// Determines whether the post-eviction callbacks of the entry stored under <paramref name="key"/> must be deferred.
    /// </summary>
    private bool IsDeferred( object key )
    {
        if ( key is not string stringKey )
        {
            return false;
        }

        lock ( this._sync )
        {
            return this._deferredCallbackKeyPredicates.Any( p => p( stringKey ) );
        }
    }

    /// <summary>
    /// Records a post-eviction callback invocation instead of running it.
    /// </summary>
    private void Capture( PostEvictionDelegate callback, object key, object? value, EvictionReason reason, object? state )
    {
        lock ( this._sync )
        {
            this._capturedCallbacks.Add( new CapturedEvictionCallback( callback, key, value, reason, state ) );
            Monitor.PulseAll( this._sync );
        }
    }

    /// <inheritdoc />
    public bool TryGetValue( object key, out object? value )
    {
        this.Intercept( InterceptedOperation.TryGetValueBefore, key );
        var found = this._inner.TryGetValue( key, out value );
        this.Intercept( InterceptedOperation.TryGetValueAfter, key, value );

        return found;
    }

    /// <inheritdoc />
    public ICacheEntry CreateEntry( object key )
    {
        this.Intercept( InterceptedOperation.CreateEntry, key );

        return new InterceptingCacheEntry( this, this._inner.CreateEntry( key ) );
    }

    /// <inheritdoc />
    public void Remove( object key )
    {
        this.Intercept( InterceptedOperation.RemoveBefore, key );
        this._inner.Remove( key );
        this.Intercept( InterceptedOperation.RemoveAfter, key );
    }

    /// <inheritdoc />
    public void Clear()
    {
        switch ( this._inner )
        {
            case MemoryCache memoryCache:
                memoryCache.Clear();

                break;

            case IClearableMemoryCache clearableMemoryCache:
                clearableMemoryCache.Clear();

                break;

            default:
                throw new NotSupportedException( "The inner cache does not support clearing." );
        }
    }

    /// <inheritdoc />
    public void Compact( double percentage )
    {
        switch ( this._inner )
        {
            case MemoryCache memoryCache:
                memoryCache.Compact( percentage );

                break;

            case IClearableMemoryCache clearableMemoryCache:
                clearableMemoryCache.Compact( percentage );

                break;

            default:
                throw new NotSupportedException( "The inner cache does not support compaction." );
        }
    }

    /// <summary>
    /// Releases every armed gate, so that no thread stays blocked, and disposes the inner cache.
    /// </summary>
    public void Dispose()
    {
        InterceptionGate[] gates;

        lock ( this._sync )
        {
            gates = this._gates.ToArray();
        }

        foreach ( var gate in gates )
        {
            gate.Release();
        }

        this._inner.Dispose();
    }
}