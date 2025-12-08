// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Services;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Testing.UnitTesting;

/// <summary>
/// Test implementation of <see cref="ITestSynchronizationProvider"/> that allows tests to control
/// execution order by blocking at named synchronization points. This enables deterministic testing
/// of concurrent code by letting tests wait for code to reach specific points and then release it.
/// </summary>
/// <remarks>
/// <para>
/// Usage pattern:
/// <list type="number">
/// <item>Register this provider with the service provider before starting the test.</item>
/// <item>Start the operation being tested (it will block at sync points).</item>
/// <item>Call <see cref="WaitForSyncPointReachedAsync"/> to wait until the code reaches the sync point.</item>
/// <item>Perform any setup needed (e.g., register an extension) while code is blocked.</item>
/// <item>Call <see cref="ReleaseSyncPoint"/> to let the code continue.</item>
/// </list>
/// </para>
/// <para>
/// Always call <see cref="ReleaseAll"/> in test cleanup to avoid deadlocks if a test fails.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class TestSynchronizationProvider : ITestSynchronizationProvider
{
    private readonly ConcurrentDictionary<string, SyncPoint> _syncPoints = new();

    /// <summary>
    /// Called by code under test at a synchronization point.
    /// Signals that the sync point was reached and waits for the test to release it.
    /// </summary>
    /// <param name="syncPointName">The name of the sync point.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SyncPointAsync( string syncPointName, CancellationToken cancellationToken )
    {
        var sp = this._syncPoints.GetOrAdd( syncPointName, _ => new SyncPoint() );
        sp.ReachedSignal.Release();
        await sp.ReleaseSignal.WaitAsync( cancellationToken );
    }

    /// <summary>
    /// Called by test code. Waits until the code under test reaches the named sync point.
    /// </summary>
    /// <param name="syncPointName">The name of the sync point to wait for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WaitForSyncPointReachedAsync( string syncPointName, CancellationToken cancellationToken )
    {
        var sp = this._syncPoints.GetOrAdd( syncPointName, _ => new SyncPoint() );
        await sp.ReachedSignal.WaitAsync( cancellationToken );
    }

    /// <summary>
    /// Called by test code. Releases the code blocked at the named sync point.
    /// </summary>
    /// <param name="syncPointName">The name of the sync point to release.</param>
    public void ReleaseSyncPoint( string syncPointName )
    {
        if ( this._syncPoints.TryGetValue( syncPointName, out var sp ) )
        {
            sp.ReleaseSignal.Release();
        }
    }

    /// <summary>
    /// Releases all sync points. Call this in test cleanup to avoid deadlocks if a test fails.
    /// </summary>
    public void ReleaseAll()
    {
        foreach ( var sp in this._syncPoints.Values )
        {
            // Release multiple times in case multiple threads are waiting.
            for ( var i = 0; i < 10; i++ )
            {
                sp.ReleaseSignal.Release();
            }
        }
    }

    private sealed class SyncPoint
    {
        public SemaphoreSlim ReachedSignal { get; } = new( 0, int.MaxValue );

        public SemaphoreSlim ReleaseSignal { get; } = new( 0, int.MaxValue );
    }
}
