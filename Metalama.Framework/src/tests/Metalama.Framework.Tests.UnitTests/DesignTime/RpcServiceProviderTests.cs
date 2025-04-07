// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
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

    [Fact]
    public async Task Test()
    {
        using var testContext = this.CreateTestContext();

        var serviceProvider = testContext.ServiceProvider.Global
            .Underlying.WithUntypedService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );

        var clientExtensionManager = new DesignTimeExtensionManager( serviceProvider );

        var pipename = $"{nameof(RpcServiceProviderTests)}_{Guid.NewGuid()}";

        // Start the server.
        var serverEndpoint = new RpcServiceProviderServerEndpoint( serviceProvider, pipename, [] );
        serverEndpoint.Start();

        // Start the client.
        var clientEndpoint = new RpcServiceProviderClientEndpoint( serviceProvider.WithService( clientExtensionManager ), pipename );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );
        await clientEndpoint.WaitUntilInitializedAsync( testContext.CancellationToken );

        // Add a service _before_ registering the extension on client side.
        var extensionServiceFactory = new ExtensionServiceFactory();
        serverEndpoint.AddServices( [extensionServiceFactory] );
        await serverEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );

        // The service is not expected to be available at this moment.
        Assert.False( clientEndpoint.IsClientAvailable<ExtensionServiceClient>() );

        // Register the extension.
        clientExtensionManager.OnExtensionDiscovered( new Extension() );

        Assert.True( clientEndpoint.IsClientAvailable<ExtensionServiceClient>() );
    }

    private sealed class Extension : IDesignTimeExtension
    {
        public bool Initialize( DesignTimeInitializationContext context ) => true;

        public string Name => _extensionName;
    }

    private interface IExtensionService : IRpcApi
    {
        void Hello();
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

            public void Hello() => this._root.OnHelloCalled();
        }
    }

    private sealed class ExtensionServiceClient : RpcClient<IExtensionService>
    {
        public ExtensionServiceClient( ClientEndpoint endpoint ) : base( endpoint ) { }
    }

    private sealed class ExtensionServiceFactory : IRpcServiceFactory
    {
        public string? ExtensionName => _extensionName;

        public RpcService CreateRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint endpoint ) => new ExtensionServiceImpl( endpoint, this );

        public RpcClient CreateRpcClient( GlobalServiceProvider serviceProvider, ClientEndpoint endpoint ) => new ExtensionServiceClient( endpoint );

        public void OnHelloCalled()
        {
            this.IsHelloMethodCalled = true;
        }

        public bool IsHelloMethodCalled { get; private set; }
    }
}