// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code

using Metalama.Framework.Engine.Utilities.Threading;
using System;
using System.Threading.Tasks;
using Xunit;

// ReSharper disable AccessToDisposedClosure

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcClientTests
{
    /// <summary>
    /// Regression test for #1640: a lost-wakeup race between <c>GetOrWaitForClientAsync</c> and
    /// <c>ConnectCoreAsync</c>. The test deterministically forces the bad interleaving — a waiter observes no
    /// client and is then suspended (still holding the add-client lock) while the connection publishes the
    /// client and signals awaiters — and asserts the waiter is still released.
    /// </summary>
    [Fact]
    public async Task GetOrWaitForClient_SignalBeforeAwaiterRegistered_StillCompletes()
    {
        using var testContext = this.CreateRpcTestContext();
        var ct = testContext.CancellationToken;

        var pipeName = $"{nameof(RpcClientTests)}_{Guid.NewGuid()}";

        // Pins the connection after StartListening but BEFORE it publishes the client to _connectionByStream.
        var afterStartsListening = $"ClientEndpoint.AfterStartsListening:{pipeName}";

        // Pins the waiter inside _addClientLock, after GetClient() returned null but before it registers its awaiter.
        var beforeRegisterAwaiter = $"ClientEndpoint.BeforeRegisterAwaiter:{pipeName}";

        using var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        using var clientEndpoint = new TestClientEndpoint( testContext.ServiceProvider, pipeName );

        // (1) Start connecting and pin it just past StartListening but before it publishes the client. At this
        // point ConnectCoreAsync has already released _addClientLock, so the waiter can acquire it, and the
        // client is not yet visible to GetClient().
        testContext.SyncProvider.EnableSyncPoint( afterStartsListening );
        var connectTask = Task.Run( () => clientEndpoint.ConnectAsync( ct ), ct );
        await testContext.SyncProvider.WaitForSyncPointReachedAsync( afterStartsListening, ct );

        // (2) Start a waiter. Under _addClientLock it sees no client (not published yet) and suspends at
        // BeforeRegisterAwaiter, still holding _addClientLock, before registering its awaiter.
        testContext.SyncProvider.EnableSyncPoint( beforeRegisterAwaiter );
        var getApiTask = Task.Run( () => clientEndpoint.GetOrWaitForClientAsync<TestClient>( ct ).AsTask(), ct );
        await testContext.SyncProvider.WaitForSyncPointReachedAsync( beforeRegisterAwaiter, ct );

        // (3) Let the connection proceed. It publishes the client and then signals awaiters.
        //   - Before the fix, signaling is lock-free: it runs now (no awaiter registered yet → wakeup lost) and
        //     ConnectAsync completes while the waiter is still suspended.
        //   - After the fix, signaling takes _addClientLock, so ConnectAsync blocks until the waiter releases it.
        testContext.SyncProvider.ReleaseSyncPoint( afterStartsListening );

        // (4) Distinguish the two states without a race: ConnectAsync completing while the waiter still holds the
        // lock proves signaling already ran without the lock (the bug). The bounded wait is a state probe, not a
        // timing hack — after the fix ConnectAsync is *designed* to stay blocked here until step (5), so we cannot
        // wait for it unconditionally.
        using var probeCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource( ct );
        probeCts.CancelAfter( TimeSpan.FromSeconds( 2 ) );

        var connectCompletedEarly = false;

        try
        {
            await connectTask.WithCancellation( probeCts.Token );
            connectCompletedEarly = true;
        }
        catch ( OperationCanceledException ) when ( probeCts.IsCancellationRequested && !ct.IsCancellationRequested )
        {
            // Expected with the fix: ConnectAsync is blocked on _addClientLock held by the suspended waiter.
        }

        Assert.False(
            connectCompletedEarly,
            "ConnectAsync signaled awaiters without holding _addClientLock while a waiter was registering — the lost-wakeup race is present." );

        // (5) Release the waiter so it registers its awaiter and awaits it. With the fix, ConnectAsync's signaling
        // (blocked on the lock) now runs and completes the just-registered awaiter.
        testContext.SyncProvider.ReleaseSyncPoint( beforeRegisterAwaiter );

        // The waiter MUST still be released. Before the fix it would hang here forever (the wakeup was lost) and
        // the test would fail via the test-context cancellation token.
        var client = await getApiTask.WithCancellation( ct );
        await connectTask.WithCancellation( ct );

        Assert.NotNull( client );
    }
}
