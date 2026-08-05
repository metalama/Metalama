// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Testing.Hooks.Tests;

#pragma warning disable VSTHRD200 // Use "Async" suffix - test naming convention prefers descriptive names.

/// <summary>
/// Tests <see cref="TestSynchronizationProvider"/>: that a synchronization point that has not been enabled is
/// skipped, that an enabled one blocks until the test releases it, and that nothing stays blocked after the provider
/// is disposed.
/// </summary>
public sealed class TestSynchronizationProviderTests
{
    private const string _syncPointName = "ComponentUnderTest.Operation:BeforeEffect";

    /// <summary>
    /// Creates a token that aborts the test rather than letting it hang if a synchronization point is never reached
    /// or never released. This is a watchdog, not a delay: a passing test never waits for it.
    /// </summary>
    private static CancellationTokenSource CreateWatchdog() => new( TimeSpan.FromSeconds( 30 ) );

    [Fact]
    public async Task AsyncSyncPointIsSkippedWhenNotEnabled()
    {
        using var watchdog = CreateWatchdog();
        using var provider = new TestSynchronizationProvider();

        // Nothing enabled this synchronization point, so it must not block.
        await provider.SyncPointAsync( _syncPointName, watchdog.Token );
    }

    [Fact]
    public void SyncPointIsSkippedWhenNotEnabled()
    {
        using var watchdog = CreateWatchdog();
        using var provider = new TestSynchronizationProvider();

        provider.SyncPoint( _syncPointName, watchdog.Token );
    }

    [Fact]
    public async Task AsyncSyncPointBlocksUntilReleased()
    {
        using var watchdog = CreateWatchdog();
        using var provider = new TestSynchronizationProvider();
        using var registration = watchdog.Token.Register( () => provider.ReleaseAll() );

        var afterSyncPoint = 0;

        provider.EnableSyncPoint( _syncPointName );

        var operation = Task.Run(
            async () =>
            {
                await provider.SyncPointAsync( _syncPointName, watchdog.Token );
                Interlocked.Increment( ref afterSyncPoint );
            } );

        await provider.WaitForSyncPointReachedAsync( _syncPointName, watchdog.Token );

        // The provider signals that the point was reached before waiting for its release, so the operation is now
        // blocked inside the synchronization point and cannot have gone further.
        Assert.False( operation.IsCompleted );
        Assert.Equal( 0, Volatile.Read( ref afterSyncPoint ) );

        provider.ReleaseSyncPoint( _syncPointName );

        await operation;

        Assert.Equal( 1, Volatile.Read( ref afterSyncPoint ) );
    }

    [Fact]
    public async Task SyncPointBlocksUntilReleased()
    {
        using var watchdog = CreateWatchdog();
        using var provider = new TestSynchronizationProvider();
        using var registration = watchdog.Token.Register( () => provider.ReleaseAll() );

        var afterSyncPoint = 0;

        provider.EnableSyncPoint( _syncPointName );

        var operation = Task.Run(
            () =>
            {
                provider.SyncPoint( _syncPointName, watchdog.Token );
                Interlocked.Increment( ref afterSyncPoint );
            } );

        await provider.WaitForSyncPointReachedAsync( _syncPointName, watchdog.Token );

        Assert.False( operation.IsCompleted );
        Assert.Equal( 0, Volatile.Read( ref afterSyncPoint ) );

        provider.ReleaseSyncPoint( _syncPointName );

        await operation;

        Assert.Equal( 1, Volatile.Read( ref afterSyncPoint ) );
    }

    [Fact]
    public async Task WaitForSyncPointReachedEnablesTheSyncPoint()
    {
        using var watchdog = CreateWatchdog();
        using var provider = new TestSynchronizationProvider();
        using var registration = watchdog.Token.Register( () => provider.ReleaseAll() );

        // The synchronization point is not enabled explicitly: waiting for it must be enough.
        var waitTask = provider.WaitForSyncPointReachedAsync( _syncPointName, watchdog.Token );

        var operation = Task.Run(
            async () => await provider.SyncPointAsync( _syncPointName, watchdog.Token ) );

        await waitTask;

        Assert.False( operation.IsCompleted );

        provider.ReleaseSyncPoint( _syncPointName );

        await operation;
    }

    [Fact]
    public async Task DisposeReleasesBlockedCode()
    {
        using var watchdog = CreateWatchdog();

        var provider = new TestSynchronizationProvider();

        provider.EnableSyncPoint( _syncPointName );

        var operation = Task.Run(
            async () => await provider.SyncPointAsync( _syncPointName, watchdog.Token ) );

        await provider.WaitForSyncPointReachedAsync( _syncPointName, watchdog.Token );

        // A test that fails while the code under test is blocked must not hang: disposal releases everything.
        provider.Dispose();

        await operation;
    }

    [Fact]
    public async Task TraceDelegateReceivesMessages()
    {
        using var watchdog = CreateWatchdog();

        var messages = new ConcurrentQueue<string>();
        using var provider = new TestSynchronizationProvider( messages.Enqueue );

        await provider.SyncPointAsync( _syncPointName, watchdog.Token );

        // Not string.Contains( string, StringComparison ), which does not exist in .NET Framework.
        Assert.Contains( messages, message => message.IndexOf( _syncPointName, StringComparison.Ordinal ) >= 0 );
    }
}
