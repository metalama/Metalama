// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Testing.Hooks;

/// <summary>
/// The default implementation of <see cref="ITestSynchronizationProvider"/>. It lets a test hold the code under test
/// at a named synchronization point while it does something else, so that a race can be driven into one specific
/// interleaving instead of being waited for.
/// </summary>
/// <remarks>
/// <para>
/// A synchronization point blocks only when the test has enabled it, so the many synchronization points a test does
/// not care about cost nothing. The usual sequence is: enable the synchronization point, start the operation on
/// another thread, wait until the point is reached, do whatever the test needs to happen in the middle, then release.
/// </para>
/// <para>
/// <see cref="Dispose"/> and <see cref="ReleaseAll"/> release everything, so a failing assertion does not turn into a
/// hanging test.
/// </para>
/// <para>
/// Tracing is reported through an optional delegate rather than a test framework abstraction, so that this package
/// has no dependency. Test classes typically pass the <c>WriteLine</c> method of their test output helper.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class TestSynchronizationProvider : ITestSynchronizationProvider, IDisposable
{
    /// <summary>
    /// The number of times the release signal of a synchronization point is raised when it is released wholesale,
    /// chosen to exceed the number of threads a test is expected to have blocked at a single point.
    /// </summary>
    private const int _bulkReleaseCount = 10;

    private readonly ConcurrentDictionary<string, SyncPointSignals> _syncPoints = new( StringComparer.Ordinal );
    private readonly Action<string>? _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestSynchronizationProvider"/> class.
    /// </summary>
    /// <param name="log">An optional delegate receiving trace messages, typically the <c>WriteLine</c> method of a test output helper.</param>
    public TestSynchronizationProvider( Action<string>? log = null )
    {
        this._log = log;
    }

    private void Log( string message ) => this._log?.Invoke( $"TestSynchronizationProvider: {message}" );

    /// <inheritdoc />
    public async Task SyncPointAsync( string syncPointName, CancellationToken cancellationToken = default )
    {
        if ( !this._syncPoints.TryGetValue( syncPointName, out var syncPoint ) )
        {
            // Nobody is waiting for this one, so it is not a synchronization point in this test.
            this.Log( $"SyncPointAsync '{syncPointName}': not enabled, skipping." );

            return;
        }

        this.Log( $"SyncPointAsync '{syncPointName}': reached, signaling and waiting for release." );
        syncPoint.ReachedSignal.Release();
        await syncPoint.ReleaseSignal.WaitAsync( cancellationToken );
        this.Log( $"SyncPointAsync '{syncPointName}': released, continuing." );
    }

    /// <inheritdoc />
    public void SyncPoint( string syncPointName, CancellationToken cancellationToken = default )
    {
        if ( !this._syncPoints.TryGetValue( syncPointName, out var syncPoint ) )
        {
            this.Log( $"SyncPoint '{syncPointName}': not enabled, skipping." );

            return;
        }

        this.Log( $"SyncPoint '{syncPointName}': reached, signaling and waiting for release." );
        syncPoint.ReachedSignal.Release();
        syncPoint.ReleaseSignal.Wait( cancellationToken );
        this.Log( $"SyncPoint '{syncPointName}': released, continuing." );
    }

    /// <summary>
    /// Called by test code. Enables a synchronization point, so that the code under test blocks when it reaches it.
    /// Must be called before the operation that reaches the synchronization point is started.
    /// </summary>
    /// <param name="syncPointName">The name of the synchronization point to enable.</param>
    public void EnableSyncPoint( string syncPointName )
    {
        this.Log( $"EnableSyncPoint '{syncPointName}'." );
        this._syncPoints.GetOrAdd( syncPointName, _ => new SyncPointSignals() );
    }

    /// <summary>
    /// Called by test code. Waits until the code under test reaches the named synchronization point. Enables the
    /// synchronization point if it has not been enabled yet.
    /// </summary>
    /// <param name="syncPointName">The name of the synchronization point to wait for.</param>
    /// <param name="cancellationToken">Cancellation token. It can be omitted in a test that has no token at hand.</param>
    public async Task WaitForSyncPointReachedAsync( string syncPointName, CancellationToken cancellationToken = default )
    {
        this.Log( $"WaitForSyncPointReachedAsync '{syncPointName}': waiting." );

        // GetOrAdd registers interest in this synchronization point.
        var syncPoint = this._syncPoints.GetOrAdd( syncPointName, _ => new SyncPointSignals() );
        await syncPoint.ReachedSignal.WaitAsync( cancellationToken );
        this.Log( $"WaitForSyncPointReachedAsync '{syncPointName}': synchronization point reached." );
    }

    /// <summary>
    /// Called by test code. Releases the code blocked at the named synchronization point.
    /// </summary>
    /// <param name="syncPointName">The name of the synchronization point to release.</param>
    public void ReleaseSyncPoint( string syncPointName )
    {
        this.Log( $"ReleaseSyncPoint '{syncPointName}'." );

        if ( this._syncPoints.TryGetValue( syncPointName, out var syncPoint ) )
        {
            syncPoint.ReleaseSignal.Release();
        }
    }

    /// <summary>
    /// Called by test code. Disables a synchronization point, so that the code under test no longer blocks when it
    /// reaches it, and releases whatever is blocked there at this moment.
    /// </summary>
    /// <param name="syncPointName">The name of the synchronization point to disable.</param>
    /// <remarks>
    /// <para>
    /// This is what a test uses once the interleaving it cared about has happened, when the code under test will
    /// reach the same point again an unknown number of times. Calling <see cref="ReleaseSyncPoint"/> instead would
    /// require the test to know exactly how many further times the point will be reached, because the release
    /// signal counts, and a test that guesses too low hangs.
    /// </para>
    /// <para>
    /// Removing the registration only affects the threads that reach the point afterwards, so this method also
    /// releases the threads that are already blocked at it, which have passed that test already.
    /// </para>
    /// </remarks>
    public void DisableSyncPoint( string syncPointName )
    {
        this.Log( $"DisableSyncPoint '{syncPointName}'." );

        if ( this._syncPoints.TryRemove( syncPointName, out var syncPoint ) )
        {
            Release( syncPoint );
        }
    }

    /// <summary>
    /// Releases all synchronization points. Called in test cleanup to avoid deadlocks if a test fails.
    /// </summary>
    public void ReleaseAll()
    {
        this.Log( $"ReleaseAll: releasing {this._syncPoints.Count} synchronization point(s)." );

        foreach ( var syncPoint in this._syncPoints.Values )
        {
            Release( syncPoint );
        }
    }

    /// <summary>
    /// Raises the release signal of a synchronization point enough times for every thread plausibly blocked at it
    /// to continue.
    /// </summary>
    /// <param name="syncPoint">The signals of the synchronization point.</param>
    private static void Release( SyncPointSignals syncPoint )
    {
        for ( var i = 0; i < _bulkReleaseCount; i++ )
        {
            syncPoint.ReleaseSignal.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => this.ReleaseAll();

    /// <summary>
    /// The pair of semaphores backing a single synchronization point.
    /// </summary>
    private sealed class SyncPointSignals
    {
        /// <summary>
        /// Gets the semaphore signaled by the code under test when it reaches the synchronization point.
        /// </summary>
        public SemaphoreSlim ReachedSignal { get; } = new( 0, int.MaxValue );

        /// <summary>
        /// Gets the semaphore signaled by the test to let the code under test continue.
        /// </summary>
        public SemaphoreSlim ReleaseSignal { get; } = new( 0, int.MaxValue );
    }
}
