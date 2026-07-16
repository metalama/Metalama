// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using System.Collections.Concurrent;

namespace Metalama.Patterns.Caching.Tests.Implementation;

/// <summary>
/// A blocking <see cref="ITestSynchronizationProvider"/> that lets a test force a specific interleaving of the
/// concurrent code under test, using the synchronization points already sprinkled through
/// <see cref="AwaitableEvent"/>.
/// </summary>
/// <remarks>
/// <para>
/// A sync point is "armed" for a given name. The first thread that reaches a matching point signals it and blocks
/// until the test calls <see cref="SyncPoint.Release"/>. Only the first matching call trips; later calls pass
/// through, so arming a name that the exercised code path reaches once yields a deterministic pause at exactly
/// that line.
/// </para>
/// <para>
/// This is an ordinary service, injected through the <see cref="IServiceProvider"/> given to the component under
/// test - not global mutable state - so tests using it can safely run in parallel with each other.
/// </para>
/// <para>
/// Requires the component under test to be built with the <c>DEBUG</c> symbol, because its synchronization points
/// are <c>[Conditional("DEBUG")]</c>.
/// </para>
/// </remarks>
internal sealed class TestSynchronizationProvider : ITestSynchronizationProvider, IServiceProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, SyncPoint> _syncPoints = new( StringComparer.Ordinal );

    /// <summary>
    /// Gets a value indicating whether any synchronization point was reached on this instance.
    /// </summary>
    public bool AnySyncPointReached { get; private set; }

    void ITestSynchronizationProvider.SyncPoint( string name )
    {
        this.AnySyncPointReached = true;

        if ( this._syncPoints.TryGetValue( name, out var syncPoint ) )
        {
            syncPoint.Trip();
        }
    }

    /// <summary>
    /// Determines, by probing, whether the assembly containing <see cref="AwaitableEvent"/> was built with the
    /// <c>DEBUG</c> symbol and therefore actually reaches its synchronization points.
    /// </summary>
    /// <remarks>
    /// Tests that drive sync points must skip when this returns <c>false</c>, otherwise they would fail waiting
    /// for a point that can never be reached. This probes the real behaviour rather than testing <c>#if DEBUG</c>
    /// in the test assembly: the two do not necessarily agree, because the test project defines <c>DEBUG</c> in the
    /// <c>LamaDebug</c> configuration while <c>Metalama.Patterns.Caching.Backend</c> does not.
    /// </remarks>
    public static bool AreSyncPointsEnabled()
    {
        using var probe = new TestSynchronizationProvider();

        // Set() reaches several sync points; none of them are armed, so this cannot block.
        new AwaitableEvent( EventResetMode.ManualReset, probe ).Set();

        return probe.AnySyncPointReached;
    }

    /// <summary>
    /// Arms a sync point. The next thread that reaches it will block until <see cref="SyncPoint.Release"/> is called.
    /// </summary>
    public SyncPoint Arm( string name )
    {
        var syncPoint = new SyncPoint();
        this._syncPoints[name] = syncPoint;

        return syncPoint;
    }

    /// <summary>
    /// Serves this instance as the <see cref="ITestSynchronizationProvider"/>, so it can be passed directly as the
    /// service provider of the component under test.
    /// </summary>
    object? IServiceProvider.GetService( Type serviceType )
        => serviceType == typeof(ITestSynchronizationProvider) ? this : null;

    public void Dispose()
    {
        // Make sure no thread stays blocked if a test fails before releasing.
        foreach ( var syncPoint in this._syncPoints.Values )
        {
            syncPoint.Release();
            syncPoint.Dispose();
        }
    }

    internal sealed class SyncPoint : IDisposable
    {
        private readonly SemaphoreSlim _reached = new( 0, 1 );
        private readonly SemaphoreSlim _release = new( 0, int.MaxValue );
        private int _armed = 1;

        public void Trip()
        {
            // Only the first matching call blocks.
            if ( Interlocked.Exchange( ref this._armed, 0 ) == 1 )
            {
                this._reached.Release();
                this._release.Wait();
            }
        }

        /// <summary>
        /// Waits until a thread has reached (and is blocked at) this sync point.
        /// </summary>
        public bool WaitUntilReached( TimeSpan timeout ) => this._reached.Wait( timeout );

        /// <summary>
        /// Releases the thread blocked at this sync point.
        /// </summary>
        public void Release() => this._release.Release();

        public void Dispose()
        {
            this._reached.Dispose();
            this._release.Dispose();
        }
    }
}
