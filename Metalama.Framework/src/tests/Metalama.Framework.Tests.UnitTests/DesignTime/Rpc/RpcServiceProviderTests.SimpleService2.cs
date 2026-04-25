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
    /// <summary>
    /// Second simple service interface for testing concurrent service addition.
    /// </summary>
    private interface ISimpleService2 : IRpcApi
    {
        Task PongAsync();
    }

    private sealed class SimpleService2Impl : RpcService<ISimpleService2>
    {
        public SimpleService2Impl( ServerEndpoint serverEndpoint ) : base( serverEndpoint ) { }

        protected override ISimpleService2 CreateApi( IRpcEventSender eventSender ) => new Api();

        private sealed class Api : ISimpleService2
        {
            public Task PongAsync() => Task.CompletedTask;
        }
    }

    private sealed class SimpleService2Client : RpcClient<ISimpleService2>
    {
        public SimpleService2Client( ClientEndpoint endpoint ) : base( endpoint ) { }
    }

    private sealed class SimpleService2Factory : IRpcServiceFactory
    {
        public string? ExtensionName => null;

        public RpcService CreateRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint endpoint ) => new SimpleService2Impl( endpoint );

        public RpcClient CreateRpcClient( GlobalServiceProvider serviceProvider, ClientEndpoint endpoint ) => new SimpleService2Client( endpoint );
    }
}