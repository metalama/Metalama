// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code

using Metalama.Framework.Engine.Utilities.Threading;
using System;
using System.Collections.Generic;
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

    protected override IEnumerable<Type> AdditionalContractTypes => [typeof(TestEventData)];

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
