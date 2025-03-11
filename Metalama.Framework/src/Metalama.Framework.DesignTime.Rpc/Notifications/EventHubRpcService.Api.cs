// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Concurrent;

namespace Metalama.Framework.DesignTime.Rpc.Notifications;

public sealed partial class EventHubRpcService
{
    private sealed class Api : IEventHubRpcApi
    {
        private readonly IRpcEventSender _event;
        private readonly ConcurrentDictionary<string, string> _subscriptions = new( StringComparer.Ordinal );

        public Api( IRpcEventSender eventSender )
        {
            this._event = eventSender;
        }

        public Task RaiseEventAsync( RpcEventData eventData, CancellationToken cancellationToken )
        {
            if ( this._subscriptions.ContainsKey( eventData.GetType().Name ) )
            {
                return this._event.RaiseEventAsync( eventData, cancellationToken );
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        public Task SubscribeAsync( string[] eventTypeNames, CancellationToken cancellationToken )
        {
            foreach ( var eventTypeName in eventTypeNames )
            {
                this._subscriptions.TryAdd( eventTypeName, eventTypeName );
            }

            return Task.CompletedTask;
        }
    }
}