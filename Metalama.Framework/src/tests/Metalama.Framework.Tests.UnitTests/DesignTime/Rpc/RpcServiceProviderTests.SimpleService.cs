// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Services;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcServiceProviderTests
{
    private interface ISimpleService : IRpcApi
    {
        Task PingAsync();
    }

    private sealed class SimpleServiceImpl : RpcService<ISimpleService>
    {
        private readonly SimpleServiceFactory _factory;

        public SimpleServiceImpl( ServerEndpoint serverEndpoint, SimpleServiceFactory factory ) : base( serverEndpoint )
        {
            this._factory = factory;
        }

        protected override ISimpleService CreateApi( IRpcEventSender eventSender ) => new Api( this._factory );

        private sealed class Api : ISimpleService
        {
            private readonly SimpleServiceFactory _factory;

            public Api( SimpleServiceFactory factory )
            {
                this._factory = factory;
            }

            public Task PingAsync()
            {
                this._factory.OnPingCalled();

                return Task.CompletedTask;
            }
        }
    }

    private sealed class SimpleServiceClient : RpcClient<ISimpleService>
    {
        public SimpleServiceClient( ClientEndpoint endpoint ) : base( endpoint ) { }
    }

    private sealed class SimpleServiceFactory : IRpcServiceFactory
    {
        public string? ExtensionName => null;

        public RpcService CreateRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint endpoint ) => new SimpleServiceImpl( endpoint, this );

        public RpcClient CreateRpcClient( GlobalServiceProvider serviceProvider, ClientEndpoint endpoint ) => new SimpleServiceClient( endpoint );

        public void OnPingCalled()
        {
            this.IsPingCalled = true;
        }

        public bool IsPingCalled { get; private set; }
    }
}
