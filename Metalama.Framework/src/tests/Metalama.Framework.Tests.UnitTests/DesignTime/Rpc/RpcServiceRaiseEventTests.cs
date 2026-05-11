// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Utilities.Threading;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

/// <summary>
/// Tests for <c>RpcService.RaiseEventAsync</c> race conditions and edge cases.
/// These tests verify correct handling of multiple clients, concurrent events, and disconnections.
/// </summary>
public sealed partial class RpcServiceRaiseEventTests : RpcUnitTestClass
{
    public RpcServiceRaiseEventTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Tests that RaiseEventAsync broadcasts events to all connected clients of the same service.
    /// This tests the behavior at line 93 where _clients.Values is enumerated.
    /// </summary>
    [Fact]
    public async Task RaiseEventAsync_MultipleClients_AllReceiveEvent()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServiceRaiseEventTests)}_{Guid.NewGuid()}";

        // Start server.
        using var serverEndpoint = new EventTestServerEndpoint( testContext.ServiceProvider, pipeName );

        var clientConnectedCount = 0;
        var allClientsConnectedTcs = new TaskCompletionSource<bool>();
        const int clientCount = 3;

        serverEndpoint.ClientConnected += () =>
        {
            if ( Interlocked.Increment( ref clientConnectedCount ) == clientCount )
            {
                allClientsConnectedTcs.TrySetResult( true );
            }
        };

        serverEndpoint.Start();

        // Connect multiple clients.
        var clients = new EventTestClientEndpoint[clientCount];
        var eventReceivedTcs = new TaskCompletionSource<string>[clientCount];
        var eventClients = new EventTestClient[clientCount];

        for ( var i = 0; i < clientCount; i++ )
        {
            clients[i] = new EventTestClientEndpoint( testContext.ServiceProvider, pipeName );
            eventReceivedTcs[i] = new TaskCompletionSource<string>();

            await clients[i].ConnectAsync( testContext.CancellationToken );

            eventClients[i] = clients[i].GetRequiredClient<EventTestClient>();
            var tcs = eventReceivedTcs[i];

            eventClients[i].EventReceived += e =>
            {
                if ( e is TestEventData testEvent )
                {
                    tcs.TrySetResult( testEvent.Message );
                }
            };
        }

        // Wait for all clients to be registered on server side.
        await allClientsConnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        // Trigger event from the server service (broadcasts to all clients).
        var service = await serverEndpoint.GetRequiredServiceAsync<EventTestService>( testContext.CancellationToken );
        await service.BroadcastEventAsync( "BroadcastMessage", testContext.CancellationToken );

        // All clients should receive the event.
        for ( var i = 0; i < clientCount; i++ )
        {
            var message = await eventReceivedTcs[i].Task.WithCancellation( testContext.CancellationToken );
            Assert.Equal( "BroadcastMessage", message );
        }

        // Cleanup.
        foreach ( var client in clients )
        {
            client.Dispose();
        }
    }

    /// <summary>
    /// Tests that multiple concurrent RaiseEventAsync calls all succeed.
    /// This verifies thread-safety of the _clients dictionary enumeration.
    /// </summary>
    [Fact]
    public async Task RaiseEventAsync_ConcurrentCalls_AllEventsDelivered()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServiceRaiseEventTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new EventTestServerEndpoint( testContext.ServiceProvider, pipeName );

        var clientConnectedTcs = new TaskCompletionSource<bool>();
        serverEndpoint.ClientConnected += () => clientConnectedTcs.TrySetResult( true );

        serverEndpoint.Start();

        using var clientEndpoint = new EventTestClientEndpoint( testContext.ServiceProvider, pipeName );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // Wait for server to register the client.
        await clientConnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        var eventClient = clientEndpoint.GetRequiredClient<EventTestClient>();

        // Track received events.
        var receivedMessages = new List<string>();
        var allEventsReceivedTcs = new TaskCompletionSource<bool>();
        const int eventCount = 10;

        eventClient.EventReceived += e =>
        {
            if ( e is TestEventData testEvent )
            {
                lock ( receivedMessages )
                {
                    receivedMessages.Add( testEvent.Message );

                    if ( receivedMessages.Count == eventCount )
                    {
                        allEventsReceivedTcs.TrySetResult( true );
                    }
                }
            }
        };

        var service = await serverEndpoint.GetRequiredServiceAsync<EventTestService>( testContext.CancellationToken );

        // Send multiple events concurrently.
        var sendTasks = new Task[eventCount];

        for ( var i = 0; i < eventCount; i++ )
        {
            var message = $"Event{i}";
            sendTasks[i] = service.BroadcastEventAsync( message, testContext.CancellationToken );
        }

        await Task.WhenAll( sendTasks ).WithCancellation( testContext.CancellationToken );

        // Wait for all events to be received.
        await allEventsReceivedTcs.Task.WithCancellation( testContext.CancellationToken );

        // Verify all events were received (order may vary).
        Assert.Equal( eventCount, receivedMessages.Count );

        for ( var i = 0; i < eventCount; i++ )
        {
            Assert.Contains( $"Event{i}", receivedMessages );
        }
    }

    /// <summary>
    /// Regression test for issue #1617 (and similar #1237). Reproduces the race where a new client is being
    /// registered on the server (between <c>RpcService&lt;TApi&gt;.ConfigureRpc</c> and
    /// <c>JsonRpc.StartListening</c>) while a concurrent <c>RaiseEventAsync</c> broadcasts an event.
    /// The bug: the new rpc was added to <c>_clients</c> in <c>ConfigureRpc</c> before <c>StartListening</c>,
    /// so the broadcast would invoke a callback proxy on a non-listening rpc and throw
    /// <c>"This operation is not allowed before listening for messages has started."</c>.
    /// Fix: registration into <c>_clients</c> is deferred to <c>OnRpcStarted</c>, called after <c>StartListening</c>.
    /// </summary>
    [Fact]
    public async Task RaiseEventAsync_DuringSecondClientRegistration_DoesNotInvokeOnNonListeningRpc()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServiceRaiseEventTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new EventTestServerEndpoint( testContext.ServiceProvider, pipeName );

        var firstClientConnectedTcs = new TaskCompletionSource<bool>();

        serverEndpoint.ClientConnected += () => firstClientConnectedTcs.TrySetResult( true );

        serverEndpoint.Start();

        // Connect first client and wait until it is fully registered on the server.
        using var client1 = new EventTestClientEndpoint( testContext.ServiceProvider, pipeName );
        await client1.ConnectAsync( testContext.CancellationToken );
        await firstClientConnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        var eventClient1 = client1.GetRequiredClient<EventTestClient>();
        var eventReceivedByClient1Tcs = new TaskCompletionSource<string>();

        eventClient1.EventReceived += e =>
        {
            if ( e is TestEventData testEvent )
            {
                eventReceivedByClient1Tcs.TrySetResult( testEvent.Message );
            }
        };

        // Pause the server right after ConfigureRpc but before StartListening for the second connection.
        // This is exactly the bug window: the buggy code would have already added the new rpc to _clients here,
        // so a concurrent RaiseEventAsync would attempt to invoke on a non-listening rpc.
        var syncPointName = $"ServerEndpoint.AfterConfiguresRpc:{pipeName}";
        testContext.SyncProvider.EnableSyncPoint( syncPointName );

        using var client2 = new EventTestClientEndpoint( testContext.ServiceProvider, pipeName );
        var client2ConnectTask = client2.ConnectAsync( testContext.CancellationToken );

        // Wait until the server reaches the sync point for the second connection.
        await testContext.SyncProvider.WaitForSyncPointReachedAsync( syncPointName, testContext.CancellationToken );

        // Broadcast while the second rpc is configured but not yet listening. With the fix the broadcast
        // sees only client1 in _clients and succeeds; without the fix it would throw "MustBeListening".
        var service = await serverEndpoint.GetRequiredServiceAsync<EventTestService>( testContext.CancellationToken );
        await service.BroadcastEventAsync( "DuringRegistration", testContext.CancellationToken );

        Assert.Equal( "DuringRegistration", await eventReceivedByClient1Tcs.Task.WithCancellation( testContext.CancellationToken ) );

        // Release the second connection.
        testContext.SyncProvider.ReleaseSyncPoint( syncPointName );
        await client2ConnectTask.WithCancellation( testContext.CancellationToken );
    }

    /// <summary>
    /// Regression test for Copilot review feedback on PR #1618. The disconnect handler could race with
    /// <c>OnRpcStarted</c>: if the disconnect arrived during the promotion window,
    /// <c>OnRpcDisconnected</c> would find no entry to remove and <c>OnRpcStarted</c> would proceed to
    /// insert a dead RPC into <c>_clients</c>/<c>_apis</c>. Subsequent broadcasts would then target a
    /// torn-down connection and throw.
    /// The fix records each rpc in <c>_registrations</c> with its own per-rpc lock; <c>OnRpcStarted</c>
    /// and <c>OnRpcDisconnected</c> contend on that lock for the same rpc, and the latter sets
    /// <c>IsDisconnected</c> so a paused promotion knows to skip. The test forces the race via the
    /// <c>OnPendingClientPromoting</c> hook and asserts that broadcasting after the dust settles only
    /// reaches the still-connected client.
    /// </summary>
    /// <remarks>
    /// This test also covers issue #1624 (mid-connect dispose): it pauses client2 at
    /// <c>ClientEndpoint.AfterGetsServer</c> so client2 is provably mid-connect (pipe accepted, no rpc
    /// yet) when <c>Dispose</c> is called. The test relies on <c>ClientEndpoint.Dispose</c> closing the
    /// pending pipe stream so the server can observe the disconnect — without that fix
    /// <c>client2.Dispose()</c> is a no-op in this state and the test hangs.
    /// </remarks>
    [Fact]
    public async Task OnRpcStarted_DisconnectInMiddleOfPromotion_DoesNotLeaveDeadRpcInDictionaries()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServiceRaiseEventTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new EventTestServerEndpoint( testContext.ServiceProvider, pipeName );

        var firstClientConnectedTcs = new TaskCompletionSource<bool>();
        var clientDisconnectedTcs = new TaskCompletionSource<bool>();

        serverEndpoint.ClientConnected += () => firstClientConnectedTcs.TrySetResult( true );
        serverEndpoint.ClientDisconnected += () => clientDisconnectedTcs.TrySetResult( true );

        serverEndpoint.Start();

        // Connect first client and wait for it to be fully registered on the server.
        using var client1 = new EventTestClientEndpoint( testContext.ServiceProvider, pipeName );
        await client1.ConnectAsync( testContext.CancellationToken );
        await firstClientConnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        var eventClient1 = client1.GetRequiredClient<EventTestClient>();
        var eventReceivedByClient1Tcs = new TaskCompletionSource<string>();

        eventClient1.EventReceived += e =>
        {
            if ( e is TestEventData testEvent )
            {
                eventReceivedByClient1Tcs.TrySetResult( testEvent.Message );
            }
        };

        // Install hooks on the service for the second connection's promotion.
        var service = await serverEndpoint.GetRequiredServiceAsync<EventTestService>( testContext.CancellationToken );

        var promotingReachedTcs = new TaskCompletionSource<bool>();

        // RunContinuationsAsynchronously: when we TrySetResult below, we don't want the OnRpcStarted
        // thread's continuation (the rest of the hook + the rest of OnRpcStarted) to run inline on the
        // test thread.
        var promotingReleaseTcs = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );
        var disconnectAttemptedTcs = new TaskCompletionSource<bool>();

        service.OnPendingClientPromotingHook = () =>
        {
            promotingReachedTcs.TrySetResult( true );

            // Block until the test releases us. This simulates the in-method race window.
            promotingReleaseTcs.Task.GetAwaiter().GetResult();
        };

        service.OnRpcDisconnectingHook = () =>
        {
            // Signal that a disconnect is being processed (fires before any dictionary cleanup).
            disconnectAttemptedTcs.TrySetResult( true );
        };

        // Pin client2 at AfterGetsServer: the pipe stream is created and OS-connected to the server,
        // but client2 has not yet created its rpc or added itself to _connectionByStream. This is
        // the pending state where ClientEndpoint.Dispose must still close the pipe (issue #1624).
        // The server's accept side runs independently from this sync point and will reach
        // OnRpcStarted's hook on its own.
        var clientPausedSyncPoint = $"ClientEndpoint.AfterGetsServer:{pipeName}";
        testContext.SyncProvider.EnableSyncPoint( clientPausedSyncPoint );

        // Start connecting the second client. Server enters OnRpcStarted, removes pending entry, hits hook.
        var client2 = new EventTestClientEndpoint( testContext.ServiceProvider, pipeName );
        var client2ConnectTask = client2.ConnectAsync( testContext.CancellationToken );

        // Wait until both: the server's OnRpcStarted is paused mid-promotion AND client2 is paused
        // at AfterGetsServer (so we know Dispose will exercise the pending-resource cleanup path).
        await promotingReachedTcs.Task.WithCancellation( testContext.CancellationToken );
        await testContext.SyncProvider.WaitForSyncPointReachedAsync( clientPausedSyncPoint, testContext.CancellationToken );

        // Trigger disconnection of client2 while server is paused. With the fix in place, the server's
        // OnRpcDisconnected will block on the registration lock. Without the fix, it would run concurrently
        // and find nothing to remove (since pending was already taken), leaving the dead rpc to be added by
        // the resuming OnRpcStarted. Disposing here also exercises ClientEndpoint.Dispose's pending-resource
        // cleanup: client2's pipe is registered as pending but not yet in _connectionByStream.
        client2.Dispose();

        // Wait until OnRpcDisconnected has been entered for client2's rpc.
        await disconnectAttemptedTcs.Task.WithCancellation( testContext.CancellationToken );

        // Release OnRpcStarted so it can finish promoting (and, with the fix, immediately yield the lock to
        // the waiting OnRpcDisconnected which will clean up).
        promotingReleaseTcs.TrySetResult( true );

        // Wait for the server-side disconnect handler to finish (so dictionaries are in their final state).
        await clientDisconnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        // Release client2's sync point so its connect task can unwind (it will fault because the pipe
        // is disposed; that's acceptable, see the catch below).
        testContext.SyncProvider.ReleaseSyncPoint( clientPausedSyncPoint );

        // Allow the connect task to complete or fail — either is acceptable, the disconnect happens client-side.
        try
        {
            // ReSharper disable once RedundantWithCancellation
            await client2ConnectTask.WithCancellation( testContext.CancellationToken );
        }
        catch
        {
            // Connection may fault because we disposed it during connect. Ignore.
        }

        // Broadcasting should only reach client1. Without the fix, the dead rpc would still be in _clients
        // and the broadcast would throw because invoking the IRpcCallback proxy on a disposed JsonRpc fails.
        await service.BroadcastEventAsync( "AfterRace", testContext.CancellationToken );
        Assert.Equal( "AfterRace", await eventReceivedByClient1Tcs.Task.WithCancellation( testContext.CancellationToken ) );
    }

    /// <summary>
    /// Regression test for Copilot review feedback on PR #1618. <c>OnRpcDisconnected</c> originally cleaned
    /// only <c>_clients</c> and left <c>_apis</c> populated. <c>EventHubRpcService.ForwardEventAsync</c>
    /// iterates the <see cref="RpcService{TApi}.Apis"/> property, so a disconnected subscriber would remain
    /// eligible for future notifications and its stored <c>IRpcEventSender</c> would attempt to raise events
    /// over a dead <c>JsonRpc</c> instance. The fix removes the entry from <c>_apis</c> alongside
    /// <c>_clients</c> when its <c>_registrations</c> entry is removed.
    /// </summary>
    [Fact]
    public async Task OnRpcDisconnected_RemovesEntryFromApisCollection()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServiceRaiseEventTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new EventTestServerEndpoint( testContext.ServiceProvider, pipeName );

        var clientConnectedCount = 0;
        var bothClientsConnectedTcs = new TaskCompletionSource<bool>();
        var clientDisconnectedTcs = new TaskCompletionSource<bool>();

        serverEndpoint.ClientConnected += () =>
        {
            if ( Interlocked.Increment( ref clientConnectedCount ) == 2 )
            {
                bothClientsConnectedTcs.TrySetResult( true );
            }
        };

        serverEndpoint.ClientDisconnected += () => clientDisconnectedTcs.TrySetResult( true );

        serverEndpoint.Start();

        using var client1 = new EventTestClientEndpoint( testContext.ServiceProvider, pipeName );
        var client2 = new EventTestClientEndpoint( testContext.ServiceProvider, pipeName );

        await client1.ConnectAsync( testContext.CancellationToken );
        await client2.ConnectAsync( testContext.CancellationToken );

        await bothClientsConnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        var eventClient1 = client1.GetRequiredClient<EventTestClient>();
        var eventReceivedByClient1Tcs = new TaskCompletionSource<string>();

        eventClient1.EventReceived += e =>
        {
            if ( e is TestEventData testEvent )
            {
                eventReceivedByClient1Tcs.TrySetResult( testEvent.Message );
            }
        };

        // Disconnect client2. The server's OnRpcDisconnected must remove the entry from _apis.
        client2.Dispose();
        await clientDisconnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        // Forward a notification through the Apis collection (mirrors EventHubRpcService.ForwardEventAsync).
        // Without the fix, client2's Api remains in _apis and its stored IRpcEventSender throws when invoked
        // on the dead JsonRpc. With the fix, only client1's Api is iterated and the broadcast succeeds.
        var service = await serverEndpoint.GetRequiredServiceAsync<EventTestService>( testContext.CancellationToken );
        await service.BroadcastViaApisAsync( "AfterDisconnect", testContext.CancellationToken );

        Assert.Equal( "AfterDisconnect", await eventReceivedByClient1Tcs.Task.WithCancellation( testContext.CancellationToken ) );
    }

    /// <summary>
    /// Regression test for Copilot review feedback on PR #1618 (commit 7bfeece). With a single per-service
    /// <c>_registrationLock</c>, a disconnect on client A serializes behind an unrelated client B's
    /// <c>OnRpcStarted</c> — so A cannot be removed from <c>_clients</c>/<c>_apis</c> until B finishes
    /// promotion. Concurrent broadcasts (which do not take the lock) can still pick up A's already-dead
    /// callback. The fix replaces the per-service lock with a per-rpc lock on a <c>RegistrationEntry</c>,
    /// so disconnect cleanup of unrelated rpcs is no longer serialized with any ongoing promotion.
    /// </summary>
    [Fact]
    public async Task OnRpcDisconnected_OfClientA_NotBlockedByOnRpcStartedOfUnrelatedClientB()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServiceRaiseEventTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new EventTestServerEndpoint( testContext.ServiceProvider, pipeName );

        var clientAConnectedTcs = new TaskCompletionSource<bool>();
        var clientDisconnectedTcs = new TaskCompletionSource<bool>();

        serverEndpoint.ClientConnected += () => clientAConnectedTcs.TrySetResult( true );
        serverEndpoint.ClientDisconnected += () => clientDisconnectedTcs.TrySetResult( true );

        serverEndpoint.Start();

        // Connect client A and wait until it is fully promoted into _clients/_apis on the server.
        var clientA = new EventTestClientEndpoint( testContext.ServiceProvider, pipeName );
        await clientA.ConnectAsync( testContext.CancellationToken );
        await clientAConnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        var service = await serverEndpoint.GetRequiredServiceAsync<EventTestService>( testContext.CancellationToken );

        // Pause client B's OnRpcStarted inside the per-rpc registration lock. With the buggy per-service
        // lock, this would block ANY other rpc's OnRpcDisconnected from running. With the per-rpc lock fix,
        // only client B's own disconnect would be blocked — client A's cleanup is unaffected.
        var bPromotingReachedTcs = new TaskCompletionSource<bool>();
        var bPromotingReleaseTcs = new TaskCompletionSource<bool>();

        service.OnPendingClientPromotingHook = () =>
        {
            bPromotingReachedTcs.TrySetResult( true );
            bPromotingReleaseTcs.Task.GetAwaiter().GetResult();
        };

        using var clientB = new EventTestClientEndpoint( testContext.ServiceProvider, pipeName );
        var clientBConnectTask = clientB.ConnectAsync( testContext.CancellationToken );

        // Wait until client B's OnRpcStarted has acquired its per-rpc lock and is paused inside the hook.
        await bPromotingReachedTcs.Task.WithCancellation( testContext.CancellationToken );

        // Disconnect client A while B's promotion is paused. The fix lets A's OnRpcDisconnected run
        // immediately (different per-rpc lock); the bug would block it behind the per-service lock.
        clientA.Dispose();

        // With the fix, ClientDisconnected fires promptly. Without the fix, this wait hangs until the
        // test's CancellationToken cancels — which is the symptom of the regression.
        await clientDisconnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        // Sanity check: with A removed from both _clients and _apis, broadcasting picks up no live
        // clients and must not throw. (B is still paused mid-promotion so it is also not in the
        // live dictionaries.)
        await service.BroadcastEventAsync( "AfterARemoved", testContext.CancellationToken );
        await service.BroadcastViaApisAsync( "AfterARemoved", testContext.CancellationToken );

        // Release client B's promotion so the connect task can complete and the test exits cleanly.
        bPromotingReleaseTcs.TrySetResult( true );

        try
        {
            // ReSharper disable once RedundantWithCancellation
            await clientBConnectTask.WithCancellation( testContext.CancellationToken );
        }
        catch
        {
            // The connect task may complete or fail — either is acceptable for this test.
        }
    }

    /// <summary>
    /// Regression test for Copilot review feedback on PR #1618 (round 4). The per-rpc lock added in
    /// <c>OnRpcStarted</c>/<c>OnRpcDisconnected</c> only narrows the race between broadcast and disconnect —
    /// it cannot eliminate it because <see cref="RpcService{TApi}.RaiseEventAsync"/> and
    /// <see cref="RpcService{TApi}.Apis"/> iteration are unlocked by design (taking the lock at broadcast
    /// time would block disconnect cleanup behind a network call). A snapshot of <c>_clients</c>/<c>_apis</c>
    /// taken just before or during cleanup can still observe a stale callback whose underlying <c>JsonRpc</c>
    /// is dead, and invoking it would throw out of <see cref="Task.WhenAll(IEnumerable{Task})"/>.
    /// The fix wraps each per-client invocation in <c>RpcService.SafeBroadcastInvokeAsync</c>, which swallows
    /// any per-client failure (logged as a warning) so a single transitioning client cannot fail the broadcast
    /// for the others. This unit test exercises the helper directly (via an <c>EventTestService</c> facade)
    /// with stub Funcs that throw a representative range of exception types — both lifecycle exceptions known
    /// to arise during the broadcast/disconnect race and an unrelated exception, all of which must be swallowed.
    /// </summary>
    [Theory]

    // StreamJsonRpc types are ILMerged into Metalama.Framework.DesignTime.Rpc and not directly visible from
    // the test assembly, so they are instantiated reflectively from the merged assembly.
    [InlineData( "StreamJsonRpc.ConnectionLostException", null )]
    [InlineData( "System.ObjectDisposedException", "rpc" )]
    [InlineData( "System.InvalidOperationException", "This operation is not allowed before listening for messages has started." )]
    [InlineData( "StreamJsonRpc.RemoteInvocationException", "This operation is not allowed before listening for messages has started." )]
    [InlineData( "System.NullReferenceException", null )]
    public async Task SafeBroadcastInvokeAsync_PerClientFailure_IsSwallowed( string exceptionTypeName, string? message )
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServiceRaiseEventTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new EventTestServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        var service = await serverEndpoint.GetRequiredServiceAsync<EventTestService>( testContext.CancellationToken );

        var toThrow = CreateException( exceptionTypeName, message );

        // The helper must not propagate the per-client failure: any exception type is swallowed (logged as
        // a warning) so the broadcast can complete for other clients.
        await service.TestSafeBroadcastInvokeAsync( _ => throw toThrow, testContext.CancellationToken );
    }

    private static Exception CreateException( string typeName, string? message )
    {
        // StreamJsonRpc.* types are not directly referenced by the test assembly. In dev builds they live in
        // a separate StreamJsonRpc.dll deployed alongside the test; in release/CI builds they are ILMerged
        // into Metalama.Framework.DesignTime.Rpc.dll. Try both sources before giving up.
        Type? type;

        if ( typeName.StartsWith( "StreamJsonRpc.", StringComparison.Ordinal ) )
        {
            type = typeof(RpcService).Assembly.GetType( typeName )
                   ?? Type.GetType( $"{typeName}, StreamJsonRpc" )
                   ?? Assembly.Load( new AssemblyName( "StreamJsonRpc" ) ).GetType( typeName );
        }
        else
        {
            type = Type.GetType( typeName );
        }

        if ( type == null )
        {
            throw new InvalidOperationException( $"Type not found: {typeName}" );
        }

        // RemoteInvocationException's only public constructors take (message, errorCode, errorData) or
        // similar — there is no plain (string) overload. Pass a non-null errorData object to disambiguate
        // from the (message, errorCode, Exception innerException) overload which would NRE on null.
        if ( typeName == "StreamJsonRpc.RemoteInvocationException" )
        {
            return (Exception) Activator.CreateInstance( type, message, 0, new object() )!;
        }

        return message is null
            ? (Exception) Activator.CreateInstance( type )!
            : (Exception) Activator.CreateInstance( type, message )!;
    }

    /// <summary>
    /// The helper must propagate <see cref="OperationCanceledException"/> when the broadcast's own
    /// cancellation token is signaled — otherwise the caller cannot tell that the whole broadcast was
    /// cancelled. (A per-client OCE for a different reason is still swallowed by the catch-all.)
    /// </summary>
    [Fact]
    public async Task SafeBroadcastInvokeAsync_CancellationOfOwnToken_IsRethrown()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServiceRaiseEventTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new EventTestServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        var service = await serverEndpoint.GetRequiredServiceAsync<EventTestService>( testContext.CancellationToken );

        using var cts = new CancellationTokenSource();

#pragma warning disable VSTHRD103
        cts.Cancel();
#pragma warning restore VSTHRD103

        await Assert.ThrowsAnyAsync<OperationCanceledException>( () => service.TestSafeBroadcastInvokeAsync( Task.FromCanceled, cts.Token ) );
    }

    /// <summary>
    /// Regression test for issue #1624 (Fix 2B). Before the fix, <c>ClientEndpoint.Dispose</c> only
    /// closed connections that had already been published to <c>_connectionByStream</c> — which
    /// happens only after <c>rpc.StartListening</c>. Disposing while <c>ConnectCoreAsync</c> was
    /// still in flight was a silent no-op: the OS pipe stayed open, the server's <c>JsonRpc</c>
    /// never raised <c>Disconnected</c>, and any code waiting for <c>OnRpcDisconnected</c> hung
    /// forever. This test pauses client2 at <c>ClientEndpoint.AfterGetsServer</c> (pipe accepted,
    /// rpc not yet created), disposes the endpoint, and asserts that the server observes the
    /// disconnect.
    /// </summary>
    [Fact]
    public async Task ClientEndpointDispose_MidConnect_ClosesPendingPipe_ServerSeesDisconnect()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServiceRaiseEventTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new EventTestServerEndpoint( testContext.ServiceProvider, pipeName );

        var clientDisconnectedTcs = new TaskCompletionSource<bool>();
        serverEndpoint.ClientDisconnected += () => clientDisconnectedTcs.TrySetResult( true );

        serverEndpoint.Start();

        // Pause the client right after the OS pipe handshake but before rpc is constructed: only
        // the NamedPipeClientStream exists and it has not yet been added to _connectionByStream.
        var syncPointName = $"ClientEndpoint.AfterGetsServer:{pipeName}";
        testContext.SyncProvider.EnableSyncPoint( syncPointName );

        var clientEndpoint = new EventTestClientEndpoint( testContext.ServiceProvider, pipeName );

        // Fire-and-forget connect: the test disposes before the connect can finish.
        var connectTask = clientEndpoint.ConnectAsync( testContext.CancellationToken );

        await testContext.SyncProvider.WaitForSyncPointReachedAsync( syncPointName, testContext.CancellationToken );

        // Dispose mid-connect. With the fix, the pending pipe is closed and the server's JsonRpc
        // detects EOF and raises Disconnected (which fires ClientDisconnected).
        clientEndpoint.Dispose();

        testContext.SyncProvider.ReleaseSyncPoint( syncPointName );

        try
        {
            // ReSharper disable once RedundantWithCancellation
            await connectTask.WithCancellation( testContext.CancellationToken );
        }
        catch
        {
            // Expected: connection task faults because the pipe was disposed mid-connect.
        }

        // The real assertion: without the fix, this hangs until the test cancellation token cancels.
        await clientDisconnectedTcs.Task.WithCancellation( testContext.CancellationToken );
    }

    /// <summary>
    /// Tests that RaiseEventAsync handles client disconnection gracefully.
    /// When a client disconnects during event broadcast, other clients should still receive the event.
    /// </summary>
    [Fact]
    public async Task RaiseEventAsync_ClientDisconnectsDuringBroadcast_OtherClientsReceiveEvent()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServiceRaiseEventTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new EventTestServerEndpoint( testContext.ServiceProvider, pipeName );

        var clientConnectedCount = 0;
        var allClientsConnectedTcs = new TaskCompletionSource<bool>();
        var clientDisconnectedTcs = new TaskCompletionSource<bool>();
        const int clientCount = 2;

        serverEndpoint.ClientConnected += () =>
        {
            if ( Interlocked.Increment( ref clientConnectedCount ) == clientCount )
            {
                allClientsConnectedTcs.TrySetResult( true );
            }
        };

        serverEndpoint.ClientDisconnected += () =>
        {
            clientDisconnectedTcs.TrySetResult( true );
        };

        serverEndpoint.Start();

        // Connect two clients.
        using var client1 = new EventTestClientEndpoint( testContext.ServiceProvider, pipeName );
        var client2 = new EventTestClientEndpoint( testContext.ServiceProvider, pipeName );

        await client1.ConnectAsync( testContext.CancellationToken );
        await client2.ConnectAsync( testContext.CancellationToken );

        // Wait for server to register both clients.
        await allClientsConnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        var eventClient1 = client1.GetRequiredClient<EventTestClient>();

        var eventReceivedByClient1 = new TaskCompletionSource<string>();

        eventClient1.EventReceived += e =>
        {
            if ( e is TestEventData testEvent )
            {
                eventReceivedByClient1.TrySetResult( testEvent.Message );
            }
        };

        // Disconnect client2 BEFORE sending the event.
        client2.Dispose();

        // Wait for server to process the disconnection.
        await clientDisconnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        // Send event - should not throw even though client2 disconnected.
        var service = await serverEndpoint.GetRequiredServiceAsync<EventTestService>( testContext.CancellationToken );
        await service.BroadcastEventAsync( "AfterDisconnect", testContext.CancellationToken );

        // Client1 should still receive the event.
        var message = await eventReceivedByClient1.Task.WithCancellation( testContext.CancellationToken );
        Assert.Equal( "AfterDisconnect", message );
    }
}