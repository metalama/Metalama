// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code

using Metalama.Framework.Engine.Utilities.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

/// <summary>
/// Tests for <c>ServerEndpoint.AcceptNewClientAsync</c> and related functionality.
/// These tests verify correct handling of multiple clients, disconnections, and disposal scenarios.
/// </summary>
public sealed partial class ServerEndpointTests : RpcUnitTestClass
{
    public ServerEndpointTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Tests that multiple clients connecting simultaneously are all served correctly.
    /// Each client should be accepted and tracked in the server's pipe collection.
    /// </summary>
    [Fact]
    public async Task AcceptNewClientAsync_MultipleClientsConnectSimultaneously_AllServed()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(ServerEndpointTests)}_{Guid.NewGuid()}";

        // Start the server.
        using var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );

        // Track how many clients have connected on the server side.
        const int clientCount = 3;
        var connectedCount = 0;
        var allClientsConnectedTcs = new TaskCompletionSource<bool>();

        serverEndpoint.ClientConnected += () =>
        {
            if ( Interlocked.Increment( ref connectedCount ) == clientCount )
            {
                allClientsConnectedTcs.TrySetResult( true );
            }
        };

        serverEndpoint.Start();

        // Connect multiple clients concurrently.
        var clients = new TestClientEndpoint[clientCount];
        var connectTasks = new Task<bool>[clientCount];

        try
        {
            for ( var i = 0; i < clientCount; i++ )
            {
                clients[i] = new TestClientEndpoint( testContext.ServiceProvider, pipeName );
                connectTasks[i] = clients[i].ConnectAsync( testContext.CancellationToken );
            }

            // Wait for all client-side connections.
            await Task.WhenAll( connectTasks ).WithCancellation( testContext.CancellationToken );

            // All connections should succeed from client perspective.
            foreach ( var task in connectTasks )
            {
                Assert.True( await task.WithCancellation( testContext.CancellationToken ) );
            }

            // Wait for all server-side connections to complete.
            await allClientsConnectedTcs.Task.WithCancellation( testContext.CancellationToken );

            // Server should have all clients connected.
            Assert.Equal( clientCount, serverEndpoint.ClientCount );
        }
        finally
        {
            foreach ( var client in clients )
            {
                client?.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that disposing the server endpoint while waiting for a connection is handled gracefully.
    /// The pending WaitForConnectionAsync should be cancelled via DisposeCancellationToken.
    /// </summary>
    [Fact]
    public async Task AcceptNewClientAsync_ServiceDisposedDuringAccept_Handled()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(ServerEndpointTests)}_{Guid.NewGuid()}";

        // Start the server - but don't connect any clients.
        var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        // Server is now waiting for a client connection.
        Assert.Equal( 0, serverEndpoint.ClientCount );

        // Dispose the server while it's waiting.
        serverEndpoint.Dispose();

        // If we got here without hanging, the disposal was handled correctly.
        // The WaitForConnectionAsync was cancelled by DisposeCancellationToken.

        await Task.CompletedTask.WithCancellation( testContext.CancellationToken );
    }

    /// <summary>
    /// Tests that the ClientConnected event is raised when a client connects.
    /// </summary>
    [Fact]
    public async Task AcceptNewClientAsync_ClientConnects_ClientConnectedEventRaised()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(ServerEndpointTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );

        var eventRaised = new TaskCompletionSource<bool>();
        serverEndpoint.ClientConnected += () => eventRaised.TrySetResult( true );

        serverEndpoint.Start();

        // Connect a client.
        using var client = new TestClientEndpoint( testContext.ServiceProvider, pipeName );
        await client.ConnectAsync( testContext.CancellationToken );

        // Wait for the event to be raised.
        await eventRaised.Task.WithCancellation( testContext.CancellationToken );

        Assert.True( eventRaised.Task.Status == TaskStatus.RanToCompletion );
    }

    /// <summary>
    /// Tests that when a client disconnects, the server cleans up properly.
    /// The pipe should be removed from tracking and disposed.
    /// </summary>
    [Fact]
    public async Task AcceptNewClientAsync_ClientDisconnects_PipeRemovedFromTracking()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(ServerEndpointTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );

        // Use TaskCompletionSource to wait for client connection on server side.
        var clientConnectedTcs = new TaskCompletionSource<bool>();
        serverEndpoint.ClientConnected += () => clientConnectedTcs.TrySetResult( true );

        serverEndpoint.Start();

        // Connect a client.
        var client = new TestClientEndpoint( testContext.ServiceProvider, pipeName );
        await client.ConnectAsync( testContext.CancellationToken );

        // Wait for the server to fully process the connection.
        await clientConnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        // Server should have 1 client.
        Assert.Equal( 1, serverEndpoint.ClientCount );

        // Set up a completion source to detect when client count drops to 0.
        var clientDisconnectedTcs = new TaskCompletionSource<bool>();

        // Check periodically when background tasks complete.
        _ = Task.Run(
            async () =>
            {
                while ( serverEndpoint.ClientCount > 0 )
                {
                    await serverEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );
                }

                clientDisconnectedTcs.TrySetResult( true );
            },
            testContext.CancellationToken );

        // Disconnect the client by disposing it.
        client.Dispose();

        // Wait for client count to become 0.
        await clientDisconnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        Assert.Equal( 0, serverEndpoint.ClientCount );
    }

    /// <summary>
    /// Tests that using a sync point, we can pause after a client is accepted
    /// and verify the server state at that point.
    /// </summary>
    [Fact]
    public async Task AcceptNewClientAsync_UsingSyncPoint_CanVerifyStateAfterAccept()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(ServerEndpointTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new TestServerEndpoint( testContext.ServiceProvider, pipeName );

        // Enable the sync point BEFORE starting the server.
        testContext.SyncProvider.EnableSyncPoint( $"ServerEndpoint.AfterGetsClient:{pipeName}" );

        serverEndpoint.Start();

        // Start client connection in background.
        using var client = new TestClientEndpoint( testContext.ServiceProvider, pipeName );
        var connectTask = client.ConnectAsync( testContext.CancellationToken );

        // Wait for server to reach sync point (client accepted but not yet configured).
        await testContext.SyncProvider.WaitForSyncPointReachedAsync(
            $"ServerEndpoint.AfterGetsClient:{pipeName}",
            testContext.CancellationToken );

        // At this point, the client has been accepted but StartListening hasn't been called yet.
        // We can verify server state here.

        // Release the sync point.
        testContext.SyncProvider.ReleaseSyncPoint( $"ServerEndpoint.AfterGetsClient:{pipeName}" );

        // Wait for connection to complete.
        await connectTask.WithCancellation( testContext.CancellationToken );

        // Wait for server to finish processing (ClientConnected event).
        var clientConnectedTcs = new TaskCompletionSource<bool>();
        serverEndpoint.ClientConnected += () => clientConnectedTcs.TrySetResult( true );

        // If already connected, check immediately; otherwise wait.
        if ( serverEndpoint.ClientCount == 0 )
        {
            await clientConnectedTcs.Task.WithCancellation( testContext.CancellationToken );
        }

        // Verify final state.
        Assert.Equal( 1, serverEndpoint.ClientCount );
    }

    /// <summary>
    /// Tests that AddServices throws InvalidOperationException when called before Start().
    /// This reproduces the bug from GitHub issue #1242 where extension discovery
    /// during initialization can trigger AddServices before Start() is called.
    /// </summary>
    [Fact]
    public void AddServices_BeforeStart_ThrowsInvalidOperationException()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(ServerEndpointTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new TestServerEndpointWithAddServices( testContext.ServiceProvider, pipeName );

        // AddServices should throw a clear InvalidOperationException explaining Start() must be called first.
        var ex = Assert.Throws<InvalidOperationException>( () => serverEndpoint.AddServicesForTest() );
        Assert.Contains( "Start", ex.Message, StringComparison.Ordinal );
    }

    /// <summary>
    /// Tests that AddServices creates a new pipe with the correct name suffix.
    /// </summary>
    [Fact]
    public async Task AddServices_CreatesNewPipeWithCorrectName()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(ServerEndpointTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new TestServerEndpointWithAddServices( testContext.ServiceProvider, pipeName );

        // Track how many clients have connected on the server side.
        const int expectedClientCount = 2;
        var connectedCount = 0;
        var allClientsConnectedTcs = new TaskCompletionSource<bool>();

        serverEndpoint.ClientConnected += () =>
        {
            if ( Interlocked.Increment( ref connectedCount ) == expectedClientCount )
            {
                allClientsConnectedTcs.TrySetResult( true );
            }
        };

        serverEndpoint.Start();

        // Add services - this should create a new pipe with suffix "-2".
        var newPipeName = serverEndpoint.AddServicesForTest();

        // Verify the new pipe name follows the expected pattern.
        Assert.Equal( $"{pipeName}-2", newPipeName );

        // Connect to original pipe.
        using var client1 = new TestClientEndpoint( testContext.ServiceProvider, pipeName );
        await client1.ConnectAsync( testContext.CancellationToken );

        // Connect to new pipe.
        using var client2 = new TestClientEndpoint( testContext.ServiceProvider, newPipeName );
        await client2.ConnectAsync( testContext.CancellationToken );

        // Wait for all server-side connections to complete.
        await allClientsConnectedTcs.Task.WithCancellation( testContext.CancellationToken );

        // Both clients should be connected.
        Assert.Equal( expectedClientCount, serverEndpoint.ClientCount );
    }
}
