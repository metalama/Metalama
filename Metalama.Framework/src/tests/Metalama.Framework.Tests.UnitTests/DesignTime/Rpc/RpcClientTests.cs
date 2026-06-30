// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code
#pragma warning disable VSTHRD103 // Cancel synchronously blocks - CancelAsync not available on all target frameworks

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Utilities.Threading;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

/// <summary>
/// Tests for <see cref="RpcClient"/> and <see cref="RpcClient{TApi}"/> initialization races.
/// </summary>
public sealed partial class RpcClientTests : RpcUnitTestClass
{
    public RpcClientTests( ITestOutputHelper logger ) : base( logger ) { }

    protected override IEnumerable<Type> AdditionalContractTypes => [typeof(TestEventData)];

    /// <summary>
    /// Tests that multiple concurrent calls to GetApiAsync all wait and return the same API instance
    /// when the RpcClient completes initialization.
    /// </summary>
    [Fact]
    public async Task GetApiAsync_ConcurrentCalls_AllReturnSameApiInstance()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcClientTests)}_{Guid.NewGuid()}";

        // Start server.
        using var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        // Create client endpoint.
        using var clientEndpoint = new TestClientEndpoint( testContext.ServiceProvider, pipeName );

        // Start 5 concurrent GetApiAsync calls BEFORE connecting.
        var getApiTasks = new Task<ITestApi>[5];
        var tasksStarted = new TaskCompletionSource<bool>[5];
        var startSignal = new TaskCompletionSource<bool>();

        for ( var i = 0; i < 5; i++ )
        {
            tasksStarted[i] = new TaskCompletionSource<bool>();
            var index = i;

            getApiTasks[i] = Task.Run(
                async () =>
                {
                    tasksStarted[index].SetResult( true );
                    await startSignal.Task.WithCancellation( testContext.CancellationToken );

                    var client = await clientEndpoint.GetOrWaitForClientAsync<TestClient>( testContext.CancellationToken );

                    return await client.GetApiAsync( testContext.CancellationToken );
                } );
        }

        // Wait for all tasks to start.
        foreach ( var started in tasksStarted )
        {
            await started.Task.WithCancellation( testContext.CancellationToken );
        }

        // Release all tasks simultaneously.
        startSignal.SetResult( true );

        // Connect in background - this will initialize the RpcClient.
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // Wait for all GetApiAsync calls to complete.
        var results = await Task.WhenAll( getApiTasks ).WithCancellation( testContext.CancellationToken );

        // All should return the same instance.
        var firstApi = results[0];

        for ( var i = 1; i < results.Length; i++ )
        {
            Assert.Same( firstApi, results[i] );
        }
    }

    /// <summary>
    /// Tests that GetApiDangerous throws InvalidOperationException before initialization completes.
    /// </summary>
    [Fact]
    public async Task GetApiDangerous_BeforeInitialized_ThrowsInvalidOperationException()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcClientTests)}_{Guid.NewGuid()}";

        // Create client endpoint but don't connect yet.
        using var clientEndpoint = new TestClientEndpointWithExposedClient( testContext.ServiceProvider, pipeName );

        // Get the client before connecting (it was created in CreateServiceClients but not initialized).
        var client = clientEndpoint.ExposedClient;

        // GetApiDangerous should throw before initialization.
        Assert.Throws<InvalidOperationException>( () => client.GetApiDangerous() );
    }

    /// <summary>
    /// Tests that EventReceived event can be subscribed and invoked concurrently.
    /// </summary>
    [Fact]
    public async Task EventReceived_ConcurrentSubscriptionAndInvocation_Works()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcClientTests)}_{Guid.NewGuid()}";

        var clientConnectedTcs = new TaskCompletionSource<bool>();

        // Start server.
        using var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.ClientConnected += () => clientConnectedTcs.TrySetResult( true );
        serverEndpoint.Start();

        // Create and connect client.
        using var clientEndpoint = new TestClientEndpoint( testContext.ServiceProvider, pipeName );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // Wait for server to register the client.
        await clientConnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        var client = await clientEndpoint.GetOrWaitForClientAsync<TestClient>( testContext.CancellationToken );

        var eventsReceived = 0;
        var subscribeTcs = new TaskCompletionSource<bool>();
        var invokeTcs = new TaskCompletionSource<bool>();

        // Subscribe in one task.
        var subscribeTask = Task.Run(
            async () =>
            {
                subscribeTcs.SetResult( true );
                await invokeTcs.Task.WithCancellation( testContext.CancellationToken );

                // Subscribe while another thread might be invoking.
                for ( var i = 0; i < 100; i++ )
                {
                    client.EventReceived += _ => Interlocked.Increment( ref eventsReceived );
                }
            } );

        // Invoke events in another task.
        var invokeTask = Task.Run(
            async () =>
            {
                await subscribeTcs.Task.WithCancellation( testContext.CancellationToken );
                invokeTcs.SetResult( true );

                // Raise events - server broadcasts to all connected clients.
                var service = await serverEndpoint.GetRequiredServiceAsync<TestService>( testContext.CancellationToken );

                for ( var i = 0; i < 10; i++ )
                {
                    await service.RaiseTestEventAsync( new TestEventData( i ), testContext.CancellationToken );
                }
            } );

        await Task.WhenAll( subscribeTask, invokeTask ).WithCancellation( testContext.CancellationToken );

        // Wait for background tasks to complete (event processing).
        await serverEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );

        // We should have received some events, but the exact count depends on timing.
        // The key is that no exceptions were thrown during concurrent subscribe/invoke.
        this.TestOutput.WriteLine( $"Events received: {eventsReceived}" );
    }

    /// <summary>
    /// Tests that WaitUntilInitializedAsync can be cancelled.
    /// </summary>
    [Fact]
    public async Task WaitUntilInitializedAsync_Cancelled_ThrowsOperationCancelledException()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcClientTests)}_{Guid.NewGuid()}";

        // Create client endpoint but don't connect (and no server running).
        using var clientEndpoint = new TestClientEndpointWithExposedClient( testContext.ServiceProvider, pipeName );
        var client = clientEndpoint.ExposedClient;

        // Create a cancellation token that we'll cancel.
        using var cts = new CancellationTokenSource();

        // Start GetApiAsync in background (will wait on WaitUntilInitializedAsync).
        var getApiTask = client.GetApiAsync( cts.Token ).AsTask();

        // Task should NOT be completed synchronously since there's no server to initialize with.
        Assert.False( getApiTask.IsCompleted );

        // Cancel.
        cts.Cancel();

        // Should throw OperationCanceledException.
        await Assert.ThrowsAsync<OperationCanceledException>( async () => await getApiTask );
    }

    /// <summary>
    /// Tests that GetApiAsync completes immediately after initialization.
    /// </summary>
    [Fact]
    public async Task GetApiAsync_AfterInitialized_CompletesImmediately()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(RpcClientTests)}_{Guid.NewGuid()}";

        // Start server.
        using var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        // Create and connect client.
        using var clientEndpoint = new TestClientEndpoint( testContext.ServiceProvider, pipeName );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        var client = await clientEndpoint.GetOrWaitForClientAsync<TestClient>( testContext.CancellationToken );

        // After initialization, GetApiAsync should complete synchronously.
        var getApiTask = client.GetApiAsync( testContext.CancellationToken );

        // Check IsCompletedSuccessfully (synchronous completion).
        Assert.True( getApiTask.IsCompletedSuccessfully );

        var api = await getApiTask;
        Assert.NotNull( api );
    }
}
