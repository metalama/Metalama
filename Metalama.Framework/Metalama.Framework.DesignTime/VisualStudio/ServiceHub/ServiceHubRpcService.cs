// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Rpc.Notifications;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Collections.Concurrent;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceHub;

[PublicAPI]
public sealed partial class ServiceHubRpcService : RpcService<IServiceHubRpcApi>
{
    private readonly ConcurrentDictionary<string, RpcServiceProviderClientEndpoint> _registeredEndpointsByPipeName = new( StringComparer.Ordinal );
    private readonly ConcurrentDictionary<ProjectKey, RpcServiceProviderClientEndpoint> _registeredEndpointsByProject = new();
    private readonly ConcurrentDictionary<ProjectKey, TaskCompletionSource<RpcServiceProviderClientEndpoint>> _waiters = new();
    private readonly ILogger _logger;
    private readonly GlobalServiceProvider _serviceProvider;

    public ServiceHubRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint serverEndpoint ) : base( serverEndpoint )
    {
        this._serviceProvider = serviceProvider;
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(ServiceHubRpcService) );
        this.PipeName = serverEndpoint.PipeName;
    }

    public string PipeName { get; }

    protected override IServiceHubRpcApi CreateApi( IRpcEventSender eventSender ) => new Api( this );

    private Task OnEndpointAddedAsync( RpcServiceProviderClientEndpoint endpoint, CancellationToken cancellationToken )
    {
        this.EndpointAdded?.Invoke( endpoint );

        // TODO: Provide the project GUID.
        return this.RaiseEventAsync( new EndpointChangedEventData( Guid.Empty ), cancellationToken );
    }

    public event Action<RpcEventData>? EventReceived;

    internal event Action<RpcServiceProviderClientEndpoint>? EndpointAdded;

    public bool IsProjectRegistered( ProjectKey projectKey ) => this._registeredEndpointsByProject.ContainsKey( projectKey );

    internal IEnumerable<RpcServiceProviderClientEndpoint> Endpoints => this._registeredEndpointsByPipeName.Values;

    public async ValueTask<RpcServiceProviderClientEndpoint> GetEndpointAsync( ProjectKey projectKey, CancellationToken cancellationToken )
    {
        if ( !projectKey.IsMetalamaEnabled )
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectKey),
                $"Cannot get the endpoint of '{projectKey}' because Metalama is not enabled for this project." );
        }

        await this.WaitUntilInitializedAsync( cancellationToken );

        if ( !this._registeredEndpointsByProject.TryGetValue( projectKey, out var endpoint ) )
        {
            this._logger.Warning?.Log( $"The project '{projectKey}' is not registered. Waiting." );
            var waiter = this._waiters.GetOrAddNew( projectKey );
            endpoint = await waiter.Task.WithCancellation( cancellationToken );
            this._logger.Trace?.Log( $"The project '{projectKey}' is now registered. Resuming." );
        }

        return endpoint;
    }

    public async Task<T> GetApiForProjectAsync<T>( ProjectKey projectKey, CancellationToken cancellationToken )
        where T : class, IRpcApi
    {
        var endpoint = await this.GetEndpointAsync( projectKey, cancellationToken );

        return await endpoint.GetApiAsync<T>( cancellationToken );
    }

    public async Task<T> GetClientForProjectAsync<T>( ProjectKey projectKey, CancellationToken cancellationToken )
        where T : RpcClient
    {
        var endpoint = await this.GetEndpointAsync( projectKey, cancellationToken );

        return await endpoint.GetOrWaitForClientAsync<T>( cancellationToken );
    }

    private void OnEventReceived( RpcEventData eventData )
    {
        this.EventReceived?.Invoke( eventData );
    }
}