// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime;

public sealed class RpcServiceProviderTests : UnitTestClass
{
    private const string _extensionName = "TheExtension";

    public RpcServiceProviderTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Tests that the retry mechanism works correctly when extension is registered AFTER
    /// the IsCompleted check. This test previously failed due to a race condition in
    /// BaseEndpoint.ExecuteBackgroundTask where fast-completing tasks weren't tracked.
    /// </summary>
    [Fact]
    public async Task RegisterServiceBeforeExtensionRegisteredInClient_RetryMechanismWorks()
    {
        using var testContext = this.CreateTestContext();
        var syncProvider = new TestSynchronizationProvider();

        var serviceProvider = testContext.ServiceProvider.Global
            .Underlying
            .WithUntypedService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() )
            .WithService<ITestSynchronizationProvider>( _ => syncProvider );

        var clientExtensionManager = new DesignTimeExtensionManager( serviceProvider );

        var pipename = $"{nameof(RpcServiceProviderTests)}_{Guid.NewGuid()}";

        // Start the server.
        var serverEndpoint = new RpcServiceProviderServerEndpoint( serviceProvider, pipename, [] );
        serverEndpoint.Start();

        // Start the client with sync provider.
        var clientEndpoint = new RpcServiceProviderClientEndpoint( serviceProvider.WithService( clientExtensionManager ), pipename );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );
        await clientEndpoint.WaitUntilInitializedAsync( testContext.CancellationToken );

        try
        {
            // Add services - client will block at sync point.
            var extensionServiceFactory = new ExtensionServiceFactory();
            serverEndpoint.AddServices( [extensionServiceFactory] );

            // Wait for client to reach the sync point.
            await syncProvider.WaitForSyncPointReachedAsync(
                $"RpcServiceProviderClientEndpoint.BeforeExtensionCheck:{_extensionName}",
                testContext.CancellationToken );

            // Release sync point WITHOUT registering extension first.
            // This forces the retry path - client will see extension not loaded.
            syncProvider.ReleaseSyncPoint( $"RpcServiceProviderClientEndpoint.BeforeExtensionCheck:{_extensionName}" );

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
        finally
        {
            syncProvider.ReleaseAll();
        }
    }

    /// <summary>
    /// Tests the scenario where extension is registered BEFORE the client checks if it's loaded.
    /// Uses synchronization to ensure deterministic timing.
    /// </summary>
    [Fact]
    public async Task RegisterServiceBeforeExtensionRegisteredInClient_ExtensionLoadedBeforeCheck()
    {
        using var testContext = this.CreateTestContext();
        var syncProvider = new TestSynchronizationProvider();

        var serviceProvider = testContext.ServiceProvider.Global
            .Underlying
            .WithUntypedService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() )
            .WithService<ITestSynchronizationProvider>( _ => syncProvider );

        var clientExtensionManager = new DesignTimeExtensionManager( serviceProvider );

        var pipename = $"{nameof(RpcServiceProviderTests)}_{Guid.NewGuid()}";

        // Start the server.
        var serverEndpoint = new RpcServiceProviderServerEndpoint( serviceProvider, pipename, [] );
        serverEndpoint.Start();

        // Start the client with sync provider.
        var clientEndpoint = new RpcServiceProviderClientEndpoint( serviceProvider.WithService( clientExtensionManager ), pipename );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );
        await clientEndpoint.WaitUntilInitializedAsync( testContext.CancellationToken );

        try
        {
            // Add services - client will block at sync point before checking if extension is loaded.
            var extensionServiceFactory = new ExtensionServiceFactory();
            serverEndpoint.AddServices( [extensionServiceFactory] );

            // Wait for client to reach the sync point (about to check IsCompleted).
            await syncProvider.WaitForSyncPointReachedAsync(
                $"RpcServiceProviderClientEndpoint.BeforeExtensionCheck:{_extensionName}",
                testContext.CancellationToken );

            // Register extension WHILE client is paused at sync point.
            // This ensures the extension IS loaded when the IsCompleted check happens.
            clientExtensionManager.OnExtensionDiscovered( new Extension() );

            // Release the sync point - client will now see extension as loaded.
            syncProvider.ReleaseSyncPoint( $"RpcServiceProviderClientEndpoint.BeforeExtensionCheck:{_extensionName}" );

            await clientEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );

            // Service should be immediately available (no retry needed).
            Assert.True( clientEndpoint.IsClientAvailable<ExtensionServiceClient>() );

            var api = await clientEndpoint.GetApiAsync<IExtensionService>( testContext.CancellationToken );
            await api.HelloAsync();
            Assert.True( extensionServiceFactory.IsHelloMethodCalled );
        }
        finally
        {
            syncProvider.ReleaseAll();
        }
    }

    private sealed class Extension : IDesignTimeExtension
    {
        public bool Initialize( DesignTimeInitializationContext context ) => true;

        public string Name => _extensionName;
    }

    private interface IExtensionService : IRpcApi
    {
        Task HelloAsync();
    }

    private sealed class ExtensionServiceImpl : RpcService<IExtensionService>
    {
        private readonly ExtensionServiceFactory _root;

        public ExtensionServiceImpl( ServerEndpoint serverEndpoint, ExtensionServiceFactory root ) : base( serverEndpoint )
        {
            this._root = root;
        }

        protected override IExtensionService CreateApi( IRpcEventSender eventSender ) => new Api( this._root );

        private sealed class Api : IExtensionService
        {
            private readonly ExtensionServiceFactory _root;

            public Api( ExtensionServiceFactory root )
            {
                this._root = root;
            }

            public Task HelloAsync()
            {
                this._root.OnHelloCalled();

                return Task.CompletedTask;
            }
        }
    }

    private sealed class ExtensionServiceClient : RpcClient<IExtensionService>
    {
        public ExtensionServiceClient( ClientEndpoint endpoint ) : base( endpoint ) { }
    }

    private sealed class ExtensionServiceFactory : IRpcServiceFactory
    {
        public string ExtensionName => _extensionName;

        public RpcService CreateRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint endpoint ) => new ExtensionServiceImpl( endpoint, this );

        public RpcClient CreateRpcClient( GlobalServiceProvider serviceProvider, ClientEndpoint endpoint ) => new ExtensionServiceClient( endpoint );

        public void OnHelloCalled()
        {
            this.IsHelloMethodCalled = true;
        }

        public bool IsHelloMethodCalled { get; private set; }
    }
}