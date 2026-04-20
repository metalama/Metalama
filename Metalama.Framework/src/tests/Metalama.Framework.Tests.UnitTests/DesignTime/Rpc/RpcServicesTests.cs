// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Utilities.Threading;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

/// <summary>
/// Tests for RPC services dimension: multiple services, event dispatching, service lookup,
/// and adding services after connection.
/// </summary>
public sealed partial class RpcServicesTests : RpcUnitTestClass
{
    public RpcServicesTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Tests that an endpoint can host multiple services and all are accessible via GetClient.
    /// </summary>
    [Fact]
    public async Task MultipleServices_AllAccessibleViaGetClient()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServicesTests)}_{Guid.NewGuid()}";

        // Start server with two services.
        using var serverEndpoint = new MultiServiceServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        // Create client with two service clients.
        using var clientEndpoint = new MultiServiceClientEndpoint( testContext.ServiceProvider, pipeName );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // Both clients should be available.
        Assert.True( clientEndpoint.IsClientAvailable<ServiceAClient>() );
        Assert.True( clientEndpoint.IsClientAvailable<ServiceBClient>() );

        var clientA = clientEndpoint.GetClient<ServiceAClient>();
        var clientB = clientEndpoint.GetClient<ServiceBClient>();

        Assert.NotNull( clientA );
        Assert.NotNull( clientB );
    }

    /// <summary>
    /// Tests that GetClient returns null for an unregistered service.
    /// </summary>
    [Fact]
    public async Task GetClient_UnregisteredService_ReturnsNull()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServicesTests)}_{Guid.NewGuid()}";

        // Start server with only service A.
        using var serverEndpoint = new SingleServiceServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        // Create client with only service A client.
        using var clientEndpoint = new SingleServiceClientEndpoint( testContext.ServiceProvider, pipeName );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // Service A should be available, service B should not.
        Assert.True( clientEndpoint.IsClientAvailable<ServiceAClient>() );
        Assert.False( clientEndpoint.IsClientAvailable<ServiceBClient>() );

        var clientB = clientEndpoint.GetClient<ServiceBClient>();
        Assert.Null( clientB );
    }

    /// <summary>
    /// Tests that GetOrWaitForClientAsync returns immediately if client is already available.
    /// </summary>
    [Fact]
    public async Task GetOrWaitForClientAsync_ClientAlreadyAvailable_ReturnsImmediately()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServicesTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new SingleServiceServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        using var clientEndpoint = new SingleServiceClientEndpoint( testContext.ServiceProvider, pipeName );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // Client is already connected, GetOrWaitForClientAsync should complete immediately.
        var waitTask = clientEndpoint.GetOrWaitForClientAsync<ServiceAClient>( testContext.CancellationToken );
        Assert.True( waitTask.IsCompleted );

        var client = await waitTask.WithCancellation( testContext.CancellationToken );
        Assert.NotNull( client );
    }

    /// <summary>
    /// Tests that API calls work correctly through the RPC channel.
    /// </summary>
    [Fact]
    public async Task ServiceApi_CallMethod_ReturnsExpectedResult()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServicesTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new SingleServiceServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        using var clientEndpoint = new SingleServiceClientEndpoint( testContext.ServiceProvider, pipeName );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        var client = clientEndpoint.GetRequiredClient<ServiceAClient>();
        var api = await client.GetApiAsync( testContext.CancellationToken );

        var result = await api.GetServiceAValueAsync( testContext.CancellationToken );
        Assert.Equal( "ServiceA", result );
    }

    /// <summary>
    /// Tests that events from a service are dispatched to the correct client.
    /// </summary>
    [Fact]
    public async Task EventDispatching_EventSentToCorrectClient()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServicesTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new MultiServiceServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        using var clientEndpoint = new MultiServiceClientEndpoint( testContext.ServiceProvider, pipeName );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        var clientA = clientEndpoint.GetRequiredClient<ServiceAClient>();
        var clientB = clientEndpoint.GetRequiredClient<ServiceBClient>();

        var eventReceivedByA = new TaskCompletionSource<RpcEventData>();
        var eventReceivedByB = new TaskCompletionSource<RpcEventData>();

        clientA.EventReceived += e => eventReceivedByA.TrySetResult( e );
        clientB.EventReceived += e => eventReceivedByB.TrySetResult( e );

        // Trigger event from service A.
        var apiA = await clientA.GetApiAsync( testContext.CancellationToken );
        await apiA.TriggerServiceAEventAsync( "TestEventFromA", testContext.CancellationToken );

        // Wait for event on client A.
        var receivedEvent = await eventReceivedByA.Task.WithCancellation( testContext.CancellationToken );
        Assert.IsType<TestEventData>( receivedEvent );
        Assert.Equal( "TestEventFromA", ((TestEventData) receivedEvent).Message );

        // Client B should NOT have received the event.
        Assert.False( eventReceivedByB.Task.IsCompleted );
    }

    /// <summary>
    /// Tests that events from multiple services are dispatched to their respective clients.
    /// </summary>
    [Fact]
    public async Task EventDispatching_MultipleServices_EachReceivesOwnEvents()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServicesTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new MultiServiceServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        using var clientEndpoint = new MultiServiceClientEndpoint( testContext.ServiceProvider, pipeName );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        var clientA = clientEndpoint.GetRequiredClient<ServiceAClient>();
        var clientB = clientEndpoint.GetRequiredClient<ServiceBClient>();

        var eventReceivedByA = new TaskCompletionSource<RpcEventData>();
        var eventReceivedByB = new TaskCompletionSource<RpcEventData>();

        clientA.EventReceived += e => eventReceivedByA.TrySetResult( e );
        clientB.EventReceived += e => eventReceivedByB.TrySetResult( e );

        // Trigger events from both services.
        var apiA = await clientA.GetApiAsync( testContext.CancellationToken );
        var apiB = await clientB.GetApiAsync( testContext.CancellationToken );

        await apiA.TriggerServiceAEventAsync( "EventFromA", testContext.CancellationToken );
        await apiB.TriggerServiceBEventAsync( "EventFromB", testContext.CancellationToken );

        // Wait for events on both clients.
        var eventA = await eventReceivedByA.Task.WithCancellation( testContext.CancellationToken );
        var eventB = await eventReceivedByB.Task.WithCancellation( testContext.CancellationToken );

        Assert.Equal( "EventFromA", ((TestEventData) eventA).Message );
        Assert.Equal( "EventFromB", ((TestEventData) eventB).Message );
    }

    /// <summary>
    /// Tests that client ConnectAsync works when called before server Start.
    /// The client should wait for the server to become available.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_ClientConnectsBeforeServerStarts_Succeeds()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServicesTests)}_{Guid.NewGuid()}";

        // Create server but DON'T start it yet.
        using var serverEndpoint = new SingleServiceServerEndpoint( testContext.ServiceProvider, pipeName );

        // Create client endpoint.
        using var clientEndpoint = new SingleServiceClientEndpoint( testContext.ServiceProvider, pipeName );

        // Start connecting BEFORE server starts - this should wait for server.
        var connectTask = clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // Connection should NOT be completed yet (server not started).
        Assert.False( connectTask.IsCompleted, "Connect should not complete before server starts" );

        // Now start the server.
        serverEndpoint.Start();

        // Connection should now succeed.
        var result = await connectTask.WithCancellation( testContext.CancellationToken );
        Assert.True( result );

        // Verify the client is available.
        Assert.True( clientEndpoint.IsClientAvailable<ServiceAClient>() );
    }

    /// <summary>
    /// Tests that GetOrWaitForClientAsync waits for a client that is added during the wait
    /// (when ConnectAsync is called after the wait starts).
    /// </summary>
    [Fact]
    public async Task GetOrWaitForClientAsync_ClientAddedDuringWait_ReturnsClient()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServicesTests)}_{Guid.NewGuid()}";

        // Start the server.
        using var serverEndpoint = new SingleServiceServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        // Create client endpoint but don't connect yet.
        using var clientEndpoint = new SingleServiceClientEndpoint( testContext.ServiceProvider, pipeName );

        // Start waiting for the client BEFORE connecting.
        // GetOrWaitForClientAsync returns a pending ValueTask immediately since the client doesn't exist.
        var waitTask = clientEndpoint.GetOrWaitForClientAsync<ServiceAClient>( testContext.CancellationToken );

        // The wait task should NOT be completed yet (client doesn't exist).
        Assert.False( waitTask.IsCompleted, "Wait task should not be completed before ConnectAsync" );

        // Now connect - this will add the client and signal the awaiter.
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // The wait task should now complete.
        var client = await waitTask.WithCancellation( testContext.CancellationToken );

        Assert.NotNull( client );
        Assert.IsType<ServiceAClient>( client );
    }

    /// <summary>
    /// Tests that multiple waiters for the same client type all receive the client
    /// when it is added. They all share the same TaskCompletionSource awaiter.
    /// </summary>
    [Fact]
    public async Task GetOrWaitForClientAsync_MultipleWaiters_AllSignaled()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcServicesTests)}_{Guid.NewGuid()}";

        // Start the server.
        using var serverEndpoint = new SingleServiceServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        // Create client endpoint but don't connect yet.
        using var clientEndpoint = new SingleServiceClientEndpoint( testContext.ServiceProvider, pipeName );

        // Start 3 waiters for the same client type BEFORE connecting.
        // GetOrWaitForClientAsync returns a pending ValueTask immediately since the client doesn't exist.
        // All waiters share the same TaskCompletionSource via _clientAwaiters.
        var waitTask1 = clientEndpoint.GetOrWaitForClientAsync<ServiceAClient>( testContext.CancellationToken );
        var waitTask2 = clientEndpoint.GetOrWaitForClientAsync<ServiceAClient>( testContext.CancellationToken );
        var waitTask3 = clientEndpoint.GetOrWaitForClientAsync<ServiceAClient>( testContext.CancellationToken );

        // None of the wait tasks should be completed yet.
        Assert.False( waitTask1.IsCompleted, "Wait task 1 should not be completed before ConnectAsync" );
        Assert.False( waitTask2.IsCompleted, "Wait task 2 should not be completed before ConnectAsync" );
        Assert.False( waitTask3.IsCompleted, "Wait task 3 should not be completed before ConnectAsync" );

        // Now connect - this will add the client and signal all awaiters.
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // All wait tasks should now complete.
        var client1 = await waitTask1.WithCancellation( testContext.CancellationToken );
        var client2 = await waitTask2.WithCancellation( testContext.CancellationToken );
        var client3 = await waitTask3.WithCancellation( testContext.CancellationToken );

        // All should receive the same client instance.
        Assert.NotNull( client1 );
        Assert.Same( client1, client2 );
        Assert.Same( client1, client3 );
    }
}