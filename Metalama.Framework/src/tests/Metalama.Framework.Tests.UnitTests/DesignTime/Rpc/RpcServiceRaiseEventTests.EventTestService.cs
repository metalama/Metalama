// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

public sealed partial class RpcServiceRaiseEventTests
{
    /// <summary>
    /// Test RPC service that exposes event broadcasting for testing.
    /// </summary>
    internal sealed class EventTestService : RpcService<IEventTestApi>
    {
        public EventTestService( ServerEndpoint serverEndpoint ) : base( serverEndpoint ) { }

        protected override IEventTestApi CreateApi( IRpcEventSender eventSender ) => new Api();

        /// <summary>
        /// Broadcasts an event to all connected clients.
        /// This calls the protected RaiseEventAsync method.
        /// </summary>
        public Task BroadcastEventAsync( string message, CancellationToken cancellationToken )
            => this.RaiseEventAsync( new TestEventData( message ), cancellationToken );

        private sealed class Api : IEventTestApi { }
    }
}