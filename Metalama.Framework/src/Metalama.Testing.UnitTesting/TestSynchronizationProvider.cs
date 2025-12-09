// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Metalama.Testing.UnitTesting;

/// <summary>
/// Test implementation of <see cref="ITestSynchronizationProvider"/> that allows tests to control
/// execution order by blocking at named synchronization points. This enables deterministic testing
/// of concurrent code by letting tests wait for code to reach specific points and then release it.
/// </summary>
/// <remarks>
/// <para>
/// Sync points are only active (blocking) when the test has registered interest by calling
/// <see cref="WaitForSyncPointReachedAsync"/>. Unregistered sync points are skipped without blocking.
/// </para>
/// <para>
/// Usage pattern:
/// <list type="number">
/// <item>Register this provider with the service provider before starting the test.</item>
/// <item>Start the operation being tested in a background task.</item>
/// <item>Call <see cref="WaitForSyncPointReachedAsync"/> to wait until the code reaches the sync point.</item>
/// <item>Perform any setup needed (e.g., register an extension) while code is blocked.</item>
/// <item>Call <see cref="ReleaseSyncPoint"/> to let the code continue.</item>
/// </list>
/// </para>
/// <para>
/// Always call <see cref="ReleaseAll"/> in test cleanup to avoid deadlocks if a test fails.
/// </para>
/// </remarks>
#pragma warning disable LAMA0821 // Cannot expose an internal Metalama API - Exceptionally because ITestSynchronizationProvider is defined in Rpc and we don't want to reference Sdk.
public sealed class TestSynchronizationProvider : ITestSynchronizationProvider, IDisposable
#pragma warning restore LAMA0821 // Cannot expose an internal Metalama API
{
    private readonly ConcurrentDictionary<string, SyncPoint> _syncPoints = new();
    private readonly ITestOutputHelper? _testOutput;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestSynchronizationProvider"/> class.
    /// </summary>
    /// <param name="testOutput">Optional test output helper for logging sync point activity.</param>
    public TestSynchronizationProvider( ITestOutputHelper? testOutput = null )
    {
        this._testOutput = testOutput;
    }

    private void Log( string message )
    {
        this._testOutput?.WriteLine( $"TestSyncProvider: {message}" );
    }

    /// <summary>
    /// Called by code under test at a synchronization point.
    /// Only blocks if the test has registered interest by calling <see cref="WaitForSyncPointReachedAsync"/>.
    /// </summary>
    /// <param name="syncPointName">The name of the sync point.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    async Task ITestSynchronizationProvider.SyncPointAsync( string syncPointName, CancellationToken cancellationToken )
    {
        // Only block if the sync point exists (was registered by WaitForSyncPointReachedAsync).
        if ( !this._syncPoints.TryGetValue( syncPointName, out var sp ) )
        {
            // No one is waiting for this sync point, skip it.
            this.Log( $"SyncPointAsync '{syncPointName}': not enabled, skipping." );

            return;
        }

        this.Log( $"SyncPointAsync '{syncPointName}': reached, signaling and waiting for release." );
        sp.ReachedSignal.Release();
        await sp.ReleaseSignal.WaitAsync( cancellationToken );
        this.Log( $"SyncPointAsync '{syncPointName}': released, continuing." );
    }

    /// <summary>
    /// Called by test code. Enables a sync point so that code under test will block when reaching it.
    /// This must be called BEFORE starting the operation that will hit the sync point.
    /// </summary>
    /// <param name="syncPointName">The name of the sync point to enable.</param>
    public void EnableSyncPoint( string syncPointName )
    {
        this.Log( $"EnableSyncPoint '{syncPointName}'." );
        this._syncPoints.GetOrAdd( syncPointName, _ => new SyncPoint() );
    }

    /// <summary>
    /// Called by test code. Waits until the code under test reaches the named sync point.
    /// Also enables the sync point if not already enabled.
    /// </summary>
    /// <param name="syncPointName">The name of the sync point to wait for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WaitForSyncPointReachedAsync( string syncPointName, CancellationToken cancellationToken )
    {
        this.Log( $"WaitForSyncPointReachedAsync '{syncPointName}': waiting." );

        // GetOrAdd to register interest in this sync point.
        var sp = this._syncPoints.GetOrAdd( syncPointName, _ => new SyncPoint() );
        await sp.ReachedSignal.WaitAsync( cancellationToken );
        this.Log( $"WaitForSyncPointReachedAsync '{syncPointName}': sync point reached." );
    }

    /// <summary>
    /// Called by test code. Releases the code blocked at the named sync point.
    /// </summary>
    /// <param name="syncPointName">The name of the sync point to release.</param>
    public void ReleaseSyncPoint( string syncPointName )
    {
        this.Log( $"ReleaseSyncPoint '{syncPointName}'." );

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
        this.Log( $"ReleaseAll: releasing {this._syncPoints.Count} sync point(s)." );

        foreach ( var sp in this._syncPoints.Values )
        {
            // Release multiple times in case multiple threads are waiting.
            for ( var i = 0; i < 10; i++ )
            {
                sp.ReleaseSignal.Release();
            }
        }
    }

    public void Dispose()
    {
        this.ReleaseAll();
    }

    private sealed class SyncPoint
    {
        public SemaphoreSlim ReachedSignal { get; } = new( 0, int.MaxValue );

        public SemaphoreSlim ReleaseSignal { get; } = new( 0, int.MaxValue );
    }
}
