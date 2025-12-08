// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

/// <summary>
/// Tests for <see cref="ClientEndpoint"/> ConnectCoreAsync behavior,
/// including duplicate client handling and awaiter signaling order.
/// </summary>
public sealed partial class ConnectCoreAsyncTests : UnitTestClass
{
    public ConnectCoreAsyncTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Tests that adding duplicate clients via AddServiceClientsAsync is ignored.
    /// The same client type (by interface name) cannot be registered twice.
    /// </summary>
    [Fact]
    public async Task AddServiceClientsAsync_DuplicateClient_Ignored()
    {
        using var testContext = this.CreateTestContext();

        var serviceProvider = testContext.ServiceProvider.Global
            .Underlying
            .WithUntypedService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );

        var pipeName = $"{nameof(ConnectCoreAsyncTests)}_{Guid.NewGuid()}";

        // Start server with dynamic service support.
        using var serverEndpoint = new DynamicServerEndpoint( serviceProvider, pipeName );
        serverEndpoint.Start();

        // Create client endpoint that allows adding service clients dynamically.
        using var clientEndpoint = new DynamicClientEndpoint( serviceProvider, pipeName );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // Verify initial client is available.
        Assert.True( clientEndpoint.IsClientAvailable<TestServiceClient>() );
        var firstClient = clientEndpoint.GetRequiredClient<TestServiceClient>();

        // Try to add the same client type again via AddServiceClientsAsync.
        // ConnectCoreAsync should detect the duplicate and skip it.
        await clientEndpoint.AddServiceClientsForTestAsync(
            $"{pipeName}-2",
            ImmutableArray.Create<RpcClient>( new TestServiceClient( clientEndpoint ) ),
            testContext.CancellationToken );

        // The client should still be the same instance (second was skipped).
        var clientAfterDuplicate = clientEndpoint.GetRequiredClient<TestServiceClient>();
        Assert.Same( firstClient, clientAfterDuplicate );
    }

    /// <summary>
    /// Tests that GetOrWaitForClientAsync returns the client when the awaiter is signaled,
    /// and that the client is available in collections (proving collection was updated before awaiter was signaled).
    /// </summary>
    [Fact]
    public async Task ConnectCoreAsync_AwaiterSignaledAfterCollectionUpdated()
    {
        using var testContext = this.CreateTestContext();

        var serviceProvider = testContext.ServiceProvider.Global
            .Underlying
            .WithUntypedService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );

        var pipeName = $"{nameof(ConnectCoreAsyncTests)}_{Guid.NewGuid()}";

        // Start server.
        using var serverEndpoint = new DynamicServerEndpoint( serviceProvider, pipeName );
        serverEndpoint.Start();

        // Create client endpoint but don't connect yet.
        using var clientEndpoint = new DynamicClientEndpoint( serviceProvider, pipeName );

        // Set up awaiter BEFORE connecting.
        // GetOrWaitForClientAsync returns a pending ValueTask immediately since the client doesn't exist.
        var waitTask = clientEndpoint.GetOrWaitForClientAsync<TestServiceClient>( testContext.CancellationToken );
        Assert.False( waitTask.IsCompleted, "Wait task should not be completed before ConnectAsync" );

        // Connect - this will add the client and signal the awaiter.
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // The awaiter should now be signaled with the client.
        var client = await waitTask.WithCancellation( testContext.CancellationToken );
        Assert.NotNull( client );

        // Verify the client is also available via GetClient (proving collection was updated first).
        // This works because ConnectCoreAsync updates _connectionByStream at line 224 BEFORE
        // signaling awaiters at lines 228-234.
        var clientFromCollection = clientEndpoint.GetClient<TestServiceClient>();
        Assert.Same( client, clientFromCollection );
    }

    /// <summary>
    /// Tests that AddServiceClientsAsync with empty clients array returns immediately without error.
    /// </summary>
    [Fact]
    public async Task AddServiceClientsAsync_EmptyClients_ReturnsImmediately()
    {
        using var testContext = this.CreateTestContext();

        var serviceProvider = testContext.ServiceProvider.Global
            .Underlying
            .WithUntypedService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );

        var pipeName = $"{nameof(ConnectCoreAsyncTests)}_{Guid.NewGuid()}";

        // Start server.
        using var serverEndpoint = new DynamicServerEndpoint( serviceProvider, pipeName );
        serverEndpoint.Start();

        // Create and connect client.
        using var clientEndpoint = new DynamicClientEndpoint( serviceProvider, pipeName );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // Try to add empty clients array - should return immediately without connecting a new pipe.
        await clientEndpoint.AddServiceClientsForTestAsync(
            $"{pipeName}-2",
            ImmutableArray<RpcClient>.Empty,
            testContext.CancellationToken );

        // Verify original client still works.
        Assert.True( clientEndpoint.IsClientAvailable<TestServiceClient>() );
    }

    /// <summary>
    /// Tests that AddServiceClientsAsync successfully adds a new client type after the initial connection.
    /// This is the basic "happy path" test for dynamic client addition.
    /// The server has both services, but the client only connects with the first initially,
    /// then adds the second via AddServiceClientsAsync (which opens a second connection to the same server).
    /// </summary>
    [Fact]
    public async Task AddServiceClientsAsync_NewClient_SuccessfullyAdded()
    {
        using var testContext = this.CreateTestContext();

        var serviceProvider = testContext.ServiceProvider.Global
            .Underlying
            .WithUntypedService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );

        var pipeName = $"{nameof(ConnectCoreAsyncTests)}_{Guid.NewGuid()}";

        // Start server with both services.
        using var serverEndpoint = new DynamicServerEndpoint( serviceProvider, pipeName );
        serverEndpoint.Start();

        // Create client endpoint with only TestServiceClient initially.
        using var clientEndpoint = new DynamicClientEndpoint( serviceProvider, pipeName );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // Verify initial client is available, but second client is not.
        Assert.True( clientEndpoint.IsClientAvailable<TestServiceClient>() );
        Assert.False( clientEndpoint.IsClientAvailable<TestService2Client>() );

        // Add the second client type via AddServiceClientsAsync.
        // This opens a NEW connection to the SAME server pipe.
        await clientEndpoint.AddServiceClientsForTestAsync(
            pipeName,
            ImmutableArray.Create<RpcClient>( new TestService2Client( clientEndpoint ) ),
            testContext.CancellationToken );

        // Both clients should now be available.
        Assert.True( clientEndpoint.IsClientAvailable<TestServiceClient>() );
        Assert.True( clientEndpoint.IsClientAvailable<TestService2Client>() );

        // Verify the new client is usable (can get API).
        var client2 = clientEndpoint.GetRequiredClient<TestService2Client>();
        var api2 = await client2.GetApiAsync( testContext.CancellationToken );
        var result = await api2.GetValue2Async( testContext.CancellationToken );
        Assert.Equal( "TestValue2", result );
    }

    /// <summary>
    /// Tests the race condition where GetOrWaitForClientAsync is called while a client is being added.
    /// Verifies that the client is found in collections BEFORE the awaiter is signaled.
    /// This test uses the <c>ClientEndpoint.BeforeSignalingAwaiters</c> sync point.
    /// </summary>
    [Fact]
    public async Task GetOrWaitForClientAsync_CalledWhileClientBeingAdded_FindsClientInCollection()
    {
        using var testContext = this.CreateTestContext();

        var syncProvider = new TestSynchronizationProvider();

        var serviceProvider = testContext.ServiceProvider.Global
            .Underlying
            .WithUntypedService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() )
            .WithUntypedService( typeof(ITestSynchronizationProvider), syncProvider );

        var pipeName = $"{nameof(ConnectCoreAsyncTests)}_{Guid.NewGuid()}";
        var syncPointName = $"ClientEndpoint.BeforeSignalingAwaiters:{pipeName}";

        // Start server.
        using var serverEndpoint = new DynamicServerEndpoint( serviceProvider, pipeName );
        serverEndpoint.Start();

        // Create client endpoint with sync provider.
        using var clientEndpoint = new DynamicClientEndpoint( serviceProvider, pipeName );

        try
        {
            // Enable the sync point BEFORE starting the connection to ensure it will block.
            syncProvider.EnableSyncPoint( syncPointName );

            // Start connection in background - it will block at the sync point.
            var connectTask = clientEndpoint.ConnectAsync( testContext.CancellationToken );

            // Wait for the sync point to be reached (collections are updated, awaiters not yet signaled).
            this.TestOutput.WriteLine( "Waiting for sync point to be reached..." );
            await syncProvider.WaitForSyncPointReachedAsync( syncPointName, testContext.CancellationToken );
            this.TestOutput.WriteLine( "Sync point reached." );

            // At this point, the client should be in collections but awaiters haven't been signaled yet.
            // GetOrWaitForClientAsync should find the client directly in collections.
            var clientFromWait = await clientEndpoint.GetOrWaitForClientAsync<TestServiceClient>( testContext.CancellationToken );
            Assert.NotNull( clientFromWait );
            this.TestOutput.WriteLine( "Got client from GetOrWaitForClientAsync while connection was paused." );

            // Also verify GetClient finds it.
            var clientFromGet = clientEndpoint.GetClient<TestServiceClient>();
            Assert.NotNull( clientFromGet );
            Assert.Same( clientFromWait, clientFromGet );
            this.TestOutput.WriteLine( "Client is in collections as expected." );

            // Release the sync point to let connection complete.
            syncProvider.ReleaseSyncPoint( syncPointName );

            // Wait for connection to complete.
            await connectTask.WithCancellation( testContext.CancellationToken );
            this.TestOutput.WriteLine( "Connection completed." );
        }
        finally
        {
            // Always release all sync points to avoid deadlocks if the test fails.
            syncProvider.ReleaseAll();
        }
    }
}
