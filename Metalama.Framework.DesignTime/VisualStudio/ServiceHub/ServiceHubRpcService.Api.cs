// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceHub;

public sealed partial class ServiceHubRpcService
{
    private sealed class Api : IServiceHubRpcApi
    {
        private readonly ServiceHubRpcService _parent;

        public Api( ServiceHubRpcService parent )
        {
            this._parent = parent;
        }

        public async Task RegisterAnalysisServiceAsync( string pipeName, CancellationToken cancellationToken )
        {
            this._parent._logger.Trace?.Log( $"Registering the endpoint '{pipeName}'." );
            var endpoint = new RpcServiceProviderClientEndpoint( this._parent._serviceProvider, pipeName );

            // Subscribe to events.
            endpoint.EventReceived += this._parent.OnEventReceived;

            // Connect to the analysis process.
            await endpoint.ConnectAsync( cancellationToken );

            if ( this._parent._registeredEndpointsByPipeName.TryAdd( pipeName, endpoint ) )
            {
                await this._parent.OnEndpointAddedAsync( endpoint, cancellationToken );
            }
            else
            {
                this._parent._logger.Error?.Log( $"The endpoint '{pipeName}' was already registered." );
            }
        }

        public Task RegisterAnalysisServiceProjectAsync( ProjectKey projectKey, string pipeName, CancellationToken cancellationToken )
        {
            this._parent._logger.Trace?.Log( $"Registering the project '{projectKey}' for endpoint '{pipeName}'." );

            if ( this._parent._registeredEndpointsByPipeName.TryGetValue( pipeName, out var endpoint ) )
            {
                if ( !this._parent._registeredEndpointsByProject.TryAdd( projectKey, endpoint ) )
                {
                    this._parent._logger.Error?.Log( $"The project '{projectKey}' was already registered." );
                }

                // Unblock waiters.
                if ( this._parent._waiters.TryRemove( projectKey, out var waiter ) )
                {
                    waiter.SetResult( endpoint );
                }
            }
            else
            {
                this._parent._logger.Error?.Log( $"The endpoint '{pipeName}' it not registered." );
            }

            return Task.CompletedTask;
        }

        public Task UnregisterAnalysisServiceAsync( string pipeName, CancellationToken cancellationToken )
        {
            this._parent._logger.Trace?.Log( $"Unregistering the endpoint '{pipeName}'." );

            if ( this._parent._registeredEndpointsByPipeName.TryRemove( pipeName, out var endpoint ) )
            {
                endpoint.EventReceived -= this._parent.OnEventReceived;
                endpoint.Dispose();
            }
            else
            {
                this._parent._logger.Error?.Log( $"The endpoint '{pipeName}' is not registered." );
            }

            return Task.CompletedTask;
        }

        public Task UnregisterAnalysisServiceProjectAsync( ProjectKey projectKey, CancellationToken cancellationToken )
        {
            this._parent._logger.Trace?.Log( $"Unregistering the project '{projectKey}'." );

            if ( !this._parent._registeredEndpointsByProject.TryRemove( projectKey, out _ ) )
            {
                this._parent._logger.Error?.Log( $"The project '{projectKey}' is not registered." );
            }

            return Task.CompletedTask;
        }
    }
}