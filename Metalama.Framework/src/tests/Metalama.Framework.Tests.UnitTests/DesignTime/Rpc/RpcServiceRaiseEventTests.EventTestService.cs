// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System;
using System.Linq;
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

        /// <summary>
        /// Test hook invoked inside <see cref="RpcService{TApi}.OnRpcStarted"/> between removing the pending entry
        /// and adding it to the live <c>_clients</c>/<c>_apis</c> dictionaries. Used to deterministically inject
        /// a concurrent disconnect during the promotion window.
        /// </summary>
        public Action? OnPendingClientPromotingHook { get; set; }

        /// <summary>
        /// Test hook invoked at the entry of <c>OnRpcDisconnected</c>, before any dictionary cleanup. Used
        /// to observe that a disconnect is being processed even when the registration lock is currently held.
        /// </summary>
        public Action? OnRpcDisconnectingHook { get; set; }

        protected override IEventTestApi CreateApi( IRpcEventSender eventSender ) => new Api( eventSender );

        /// <summary>
        /// Broadcasts an event to all connected clients.
        /// This calls the protected RaiseEventAsync method.
        /// </summary>
        public Task BroadcastEventAsync( string message, CancellationToken cancellationToken )
            => this.RaiseEventAsync( new TestEventData( message ), cancellationToken );

        /// <summary>
        /// Broadcasts an event by iterating <see cref="RpcService{TApi}.Apis"/> (mirrors <c>EventHubRpcService.ForwardEventAsync</c>).
        /// Used by tests to verify that disconnected clients are removed from the Apis collection.
        /// </summary>
        public Task BroadcastViaApisAsync( string message, CancellationToken cancellationToken )
            => Task.WhenAll(
                this.Apis.Select( api => ((Api) api).SendEventAsync( new TestEventData( message ), cancellationToken ) ) );

        protected override void OnPendingClientPromoting() => this.OnPendingClientPromotingHook?.Invoke();

        protected override void OnRpcDisconnecting() => this.OnRpcDisconnectingHook?.Invoke();

        private sealed class Api : IEventTestApi
        {
            private readonly IRpcEventSender _eventSender;

            public Api( IRpcEventSender eventSender )
            {
                this._eventSender = eventSender;
            }

            public Task SendEventAsync( RpcEventData eventData, CancellationToken cancellationToken )
                => this._eventSender.RaiseEventAsync( eventData, cancellationToken );
        }
    }
}
