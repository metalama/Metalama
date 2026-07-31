// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Metalama.Backstage.Testing;

/// <summary>
/// Lets a test hold the code under test at a named point while it does something else, so that a race can be driven
/// into one specific interleaving instead of being waited for.
/// </summary>
/// <remarks>
/// <para>
/// A sync point only blocks when the test has enabled it, so the many sync points a test does not care about cost
/// nothing. The usual sequence is: enable the sync point, start the operation on another thread, wait until it is
/// reached, do whatever the test needs to happen in the middle, then release.
/// </para>
/// <para>
/// Dispose (or <see cref="ReleaseAll"/>) releases everything, so a failing assertion does not turn into a hanging test.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class TestSynchronizationProvider : ITestSynchronizationProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, SyncPoint> _syncPoints = new( StringComparer.Ordinal );
    private readonly ITestOutputHelper? _testOutput;

    public TestSynchronizationProvider( ITestOutputHelper? testOutput = null )
    {
        this._testOutput = testOutput;
    }

    private void Log( string message ) => this._testOutput?.WriteLine( $"TestSynchronizationProvider: {message}" );

    void ITestSynchronizationProvider.SyncPoint( string syncPointName, CancellationToken cancellationToken )
    {
        if ( !this._syncPoints.TryGetValue( syncPointName, out var syncPoint ) )
        {
            // Nobody is waiting for this one, so it is not a sync point in this test.
            return;
        }

        this.Log( $"'{syncPointName}' reached, waiting for release." );
        syncPoint.ReachedSignal.Release();
        syncPoint.ReleaseSignal.Wait( cancellationToken );
        this.Log( $"'{syncPointName}' released." );
    }

    /// <summary>
    /// Enables a sync point, so that the code under test blocks when it reaches it. Must be called before the operation
    /// that reaches it is started.
    /// </summary>
    public void EnableSyncPoint( string syncPointName )
    {
        this.Log( $"'{syncPointName}' enabled." );
        _ = this._syncPoints.GetOrAdd( syncPointName, _ => new SyncPoint() );
    }

    /// <summary>
    /// Waits until the code under test reaches the named sync point, enabling it if necessary.
    /// </summary>
    public async Task WaitForSyncPointReachedAsync( string syncPointName, CancellationToken cancellationToken = default )
    {
        var syncPoint = this._syncPoints.GetOrAdd( syncPointName, _ => new SyncPoint() );
        await syncPoint.ReachedSignal.WaitAsync( cancellationToken );
        this.Log( $"'{syncPointName}' observed as reached." );
    }

    /// <summary>
    /// Releases the code blocked at the named sync point.
    /// </summary>
    public void ReleaseSyncPoint( string syncPointName )
    {
        this.Log( $"'{syncPointName}' releasing." );

        if ( this._syncPoints.TryGetValue( syncPointName, out var syncPoint ) )
        {
            syncPoint.ReleaseSignal.Release();
        }
    }

    /// <summary>
    /// Releases every sync point, so that a failed test cannot leave a thread blocked for ever.
    /// </summary>
    public void ReleaseAll()
    {
        foreach ( var syncPoint in this._syncPoints.Values )
        {
            // Several threads may be waiting on the same sync point.
            syncPoint.ReleaseSignal.Release( 16 );
        }
    }

    public void Dispose() => this.ReleaseAll();

    private sealed class SyncPoint
    {
        public SemaphoreSlim ReachedSignal { get; } = new( 0, int.MaxValue );

        public SemaphoreSlim ReleaseSignal { get; } = new( 0, int.MaxValue );
    }
}
