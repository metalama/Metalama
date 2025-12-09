// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks - acceptable in test code

using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Utilities.Threading;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcServiceProviderTests : RpcUnitTestClass
{
    internal const string ExtensionName = "TheExtension";

    public RpcServiceProviderTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Tests that the retry mechanism works correctly when extension is registered AFTER
    /// the IsCompleted check. This test previously failed due to a race condition in
    /// BaseEndpoint.ExecuteBackgroundTask where fast-completing tasks weren't tracked.
    /// </summary>
    [Fact]
    public async Task RegisterServiceBeforeExtensionRegisteredInClient_RetryMechanismWorks()
    {
        using var testContext = this.CreateRpcTestContext();

        // ReSharper disable once UseAwaitUsing
        using var cancellationRegistration = testContext.CancellationToken.Register( () => testContext.SyncProvider.ReleaseAll() );

        var clientExtensionManager = new DesignTimeExtensionManager( testContext.Global.Underlying );

        var pipename = $"{nameof(RpcServiceProviderTests)}_{Guid.NewGuid()}";

        // Start the server.
        using var serverEndpoint = new RpcServiceProviderServerEndpoint( testContext.Global, pipename, [] );
        serverEndpoint.Start();

        // Start the client with sync provider.
        using var clientEndpoint = new RpcServiceProviderClientEndpoint( testContext.Global.WithService( clientExtensionManager ), pipename );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );
        await clientEndpoint.WaitUntilInitializedAsync( testContext.CancellationToken );

        // Enable the sync point BEFORE adding services so it will block.
        testContext.SyncProvider.EnableSyncPoint( $"RpcServiceProviderClientEndpoint.BeforeExtensionCheck:{ExtensionName}" );

        // Add services - client will block at sync point.
        var extensionServiceFactory = new ExtensionServiceFactory();
        serverEndpoint.AddServices( [extensionServiceFactory] );

        // Wait for client to reach the sync point.
        await testContext.SyncProvider.WaitForSyncPointReachedAsync(
            $"RpcServiceProviderClientEndpoint.BeforeExtensionCheck:{ExtensionName}",
            testContext.CancellationToken );

        // Release sync point WITHOUT registering extension first.
        // This forces the retry path - client will see extension not loaded.
        testContext.SyncProvider.ReleaseSyncPoint( $"RpcServiceProviderClientEndpoint.BeforeExtensionCheck:{ExtensionName}" );

        // Now register extension - retry task will complete.
        clientExtensionManager.OnExtensionDiscovered( new Extension() );

        // Wait for all background tasks including the retry.
        await clientEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );

        // Service should be available via the retry mechanism.
        Assert.True( clientEndpoint.IsClientAvailable<ExtensionServiceClient>() );

        var api = await clientEndpoint.GetApiAsync<IExtensionService>( testContext.CancellationToken );
        await api.HelloAsync();
        Assert.True( extensionServiceFactory.IsHelloMethodCalled );
    }

    /// <summary>
    /// Tests the scenario where extension is registered BEFORE the client checks if it's loaded.
    /// Uses synchronization to ensure deterministic timing.
    /// </summary>
    [Fact]
    public async Task RegisterServiceBeforeExtensionRegisteredInClient_ExtensionLoadedBeforeCheck()
    {
        using var testContext = this.CreateRpcTestContext();

        // ReSharper disable once UseAwaitUsing
        using var cancellationRegistration = testContext.CancellationToken.Register( () => testContext.SyncProvider.ReleaseAll() );

        var clientExtensionManager = new DesignTimeExtensionManager( testContext.Global.Underlying );

        var pipename = $"{nameof(RpcServiceProviderTests)}_{Guid.NewGuid()}";

        // Start the server.
        using var serverEndpoint = new RpcServiceProviderServerEndpoint( testContext.Global, pipename, [] );
        serverEndpoint.Start();

        // Start the client with sync provider.
        using var clientEndpoint = new RpcServiceProviderClientEndpoint( testContext.Global.WithService( clientExtensionManager ), pipename );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );
        await clientEndpoint.WaitUntilInitializedAsync( testContext.CancellationToken );

        // Enable the sync point BEFORE adding services so it will block.
        testContext.SyncProvider.EnableSyncPoint( $"RpcServiceProviderClientEndpoint.BeforeExtensionCheck:{ExtensionName}" );

        // Add services - client will block at sync point before checking if extension is loaded.
        var extensionServiceFactory = new ExtensionServiceFactory();
        serverEndpoint.AddServices( [extensionServiceFactory] );

        // Wait for client to reach the sync point (about to check IsCompleted).
        await testContext.SyncProvider.WaitForSyncPointReachedAsync(
            $"RpcServiceProviderClientEndpoint.BeforeExtensionCheck:{ExtensionName}",
            testContext.CancellationToken );

        // Register extension WHILE client is paused at sync point.
        // This ensures the extension IS loaded when the IsCompleted check happens.
        clientExtensionManager.OnExtensionDiscovered( new Extension() );

        // Release the sync point - client will now see extension as loaded.
        testContext.SyncProvider.ReleaseSyncPoint( $"RpcServiceProviderClientEndpoint.BeforeExtensionCheck:{ExtensionName}" );

        await clientEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );

        // Service should be immediately available (no retry needed).
        Assert.True( clientEndpoint.IsClientAvailable<ExtensionServiceClient>() );

        var api = await clientEndpoint.GetApiAsync<IExtensionService>( testContext.CancellationToken );
        await api.HelloAsync();
        Assert.True( extensionServiceFactory.IsHelloMethodCalled );
    }

    /// <summary>
    /// Tests that services without extensions are available after being added dynamically.
    /// </summary>
    [Fact]
    public async Task ServiceWithoutExtension_DynamicallyAdded_IsAvailable()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipename = $"{nameof(RpcServiceProviderTests)}_{Guid.NewGuid()}";

        // Start the server with no initial services.
        using var serverEndpoint = new RpcServiceProviderServerEndpoint( testContext.Global, pipename, [] );
        serverEndpoint.Start();

        // Start the client.
        using var clientEndpoint = new RpcServiceProviderClientEndpoint( testContext.Global, pipename );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );
        await clientEndpoint.WaitUntilInitializedAsync( testContext.CancellationToken );

        // Service should not be available yet.
        Assert.False( clientEndpoint.IsClientAvailable<SimpleServiceClient>() );

        // Add service dynamically.
        var simpleServiceFactory = new SimpleServiceFactory();
        serverEndpoint.AddServices( [simpleServiceFactory] );

        // Wait for background tasks to complete.
        await serverEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );
        await clientEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );

        // Service should now be available.
        Assert.True( clientEndpoint.IsClientAvailable<SimpleServiceClient>() );

        var api = await clientEndpoint.GetApiAsync<ISimpleService>( testContext.CancellationToken );
        await api.PingAsync();
        Assert.True( simpleServiceFactory.IsPingCalled );
    }

    /// <summary>
    /// Tests that concurrent GetApiAsync calls all return the same API instance.
    /// </summary>
    [Fact]
    public async Task GetApiAsync_ConcurrentCalls_AllReturnSameApiInstance()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipename = $"{nameof(RpcServiceProviderTests)}_{Guid.NewGuid()}";

        // Start the server with no initial services.
        using var serverEndpoint = new RpcServiceProviderServerEndpoint( testContext.Global, pipename, [] );
        serverEndpoint.Start();

        // Start the client.
        using var clientEndpoint = new RpcServiceProviderClientEndpoint( testContext.Global, pipename );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );
        await clientEndpoint.WaitUntilInitializedAsync( testContext.CancellationToken );

        // Add service dynamically.
        var simpleServiceFactory = new SimpleServiceFactory();
        serverEndpoint.AddServices( [simpleServiceFactory] );

        // Wait for background tasks to complete.
        await serverEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );
        await clientEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );

        // Start 5 concurrent GetApiAsync calls.
        var getApiTasks = new Task<ISimpleService>[5];

        for ( var i = 0; i < 5; i++ )
        {
            getApiTasks[i] = clientEndpoint.GetApiAsync<ISimpleService>( testContext.CancellationToken ).AsTask();
        }

        // Wait for all to complete.
        var results = await Task.WhenAll( getApiTasks ).WithCancellation( testContext.CancellationToken );

        // All should return the same instance.
        var firstApi = results[0];

        for ( var i = 1; i < results.Length; i++ )
        {
            Assert.Same( firstApi, results[i] );
        }
    }

    /// <summary>
    /// Demonstrates the race condition that causes GitHub issue #1242 and #27:
    /// When an extension service is added to the server, the client starts a background task
    /// that waits for the extension to be discovered via GetExtensionAsync. If the extension
    /// is never discovered in the client's DesignTimeExtensionManager, the background task
    /// never completes, causing WhenBackgroundTasksCompletedAsync to hang forever.
    ///
    /// This test is skipped because it would timeout - it demonstrates the bug rather than fixing it.
    /// </summary>
    [Fact( Skip = "Demonstrates bug #1242 - will timeout waiting for extension that is never discovered" )]
    public async Task ExtensionNeverDiscovered_BackgroundTaskNeverCompletes()
    {
        using var testContext = this.CreateRpcTestContext();

        // Create extension manager for client - but we'll never register the extension
        var clientExtensionManager = new DesignTimeExtensionManager( testContext.Global.Underlying );

        var pipename = $"{nameof(RpcServiceProviderTests)}_{Guid.NewGuid()}";

        // Start the server
        using var serverEndpoint = new RpcServiceProviderServerEndpoint( testContext.Global, pipename, [] );
        serverEndpoint.Start();

        // Start the client - it has an extension manager but the extension will never be discovered
        using var clientEndpoint = new RpcServiceProviderClientEndpoint( testContext.Global.WithService( clientExtensionManager ), pipename );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );
        await clientEndpoint.WaitUntilInitializedAsync( testContext.CancellationToken );

        // Add extension services to server - this will notify the client
        var extensionServiceFactory = new ExtensionServiceFactory();
        serverEndpoint.AddServices( [extensionServiceFactory] );

        // Wait for server to process the AddServices
        await serverEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );

        // The client started a background task that waits for the extension to be discovered.
        // Since we never call clientExtensionManager.OnExtensionDiscovered(), the background
        // task will never complete, and this will hang forever (or until CancellationToken fires).
        //
        // This demonstrates the bug - in production, this happens when:
        // - Analysis process discovers extension and adds RPC services
        // - User process client receives notification about new services
        // - User process client starts background task waiting for extension
        // - But user process never discovers the extension (OnProjectDiscovered is only called in analysis process)
        // - Background task waits forever -> deadlock
        await clientEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );

        // This line is never reached
        Assert.True( clientEndpoint.IsClientAvailable<ExtensionServiceClient>() );
    }

    /// <summary>
    /// Tests that concurrent AddServices calls on the server work correctly.
    /// </summary>
    [Fact]
    public async Task AddServices_ConcurrentCalls_AllServicesAdded()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipename = $"{nameof(RpcServiceProviderTests)}_{Guid.NewGuid()}";

        // Start the server with no initial services.
        using var serverEndpoint = new RpcServiceProviderServerEndpoint( testContext.Global, pipename, [] );
        serverEndpoint.Start();

        // Start the client.
        using var clientEndpoint = new RpcServiceProviderClientEndpoint( testContext.Global, pipename );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );
        await clientEndpoint.WaitUntilInitializedAsync( testContext.CancellationToken );

        // Create multiple service factories.
        var simpleServiceFactory = new SimpleServiceFactory();
        var simpleService2Factory = new SimpleService2Factory();

        // Add services concurrently from different threads.
        var startSignal = new TaskCompletionSource<bool>();

        var addTask1 = Task.Run(
            async () =>
            {
                await startSignal.Task.WithCancellation( testContext.CancellationToken );
                serverEndpoint.AddServices( [simpleServiceFactory] );
            } );

        var addTask2 = Task.Run(
            async () =>
            {
                await startSignal.Task.WithCancellation( testContext.CancellationToken );
                serverEndpoint.AddServices( [simpleService2Factory] );
            } );

        // Release both tasks simultaneously.
        startSignal.SetResult( true );

        await Task.WhenAll( addTask1, addTask2 ).WithCancellation( testContext.CancellationToken );

        // Wait for background tasks.
        await serverEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );
        await clientEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );

        // Both services should be available.
        Assert.True( clientEndpoint.IsClientAvailable<SimpleServiceClient>() );
        Assert.True( clientEndpoint.IsClientAvailable<SimpleService2Client>() );
    }
}
