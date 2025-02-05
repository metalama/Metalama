// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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