// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcServicesTests
{
    private sealed class ServiceAService : RpcService<IServiceAApi>
    {
        public ServiceAService( ServerEndpoint serverEndpoint ) : base( serverEndpoint ) { }

        protected override IServiceAApi CreateApi( IRpcEventSender eventSender ) => new Api( this, eventSender );

        private sealed class Api : IServiceAApi
        {
            private readonly IRpcEventSender _eventSender;

            public Api( ServiceAService service, IRpcEventSender eventSender )
            {
                _ = service;
                this._eventSender = eventSender;
            }

            public Task<string> GetServiceAValueAsync( CancellationToken cancellationToken ) => Task.FromResult( "ServiceA" );

            public async Task TriggerServiceAEventAsync( string message, CancellationToken cancellationToken )
            {
                await this._eventSender.RaiseEventAsync( new TestEventData( message ), cancellationToken );
            }
        }
    }
}