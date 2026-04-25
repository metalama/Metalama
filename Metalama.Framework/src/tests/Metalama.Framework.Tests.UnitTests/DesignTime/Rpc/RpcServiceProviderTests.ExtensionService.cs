// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Services;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcServiceProviderTests
{
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