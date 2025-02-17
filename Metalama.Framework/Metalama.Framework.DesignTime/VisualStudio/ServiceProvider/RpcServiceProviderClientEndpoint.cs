// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Services;
using Microsoft.VisualStudio.Threading;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;

/// <summary>
/// Implements the client endpoint for <see cref="IRpcServiceProviderApi"/>. Runs in the user process.
/// </summary>
public sealed class RpcServiceProviderClientEndpoint : ClientEndpoint
{
    private readonly GlobalServiceProvider _serviceProvider;
    private readonly RpcServiceProviderClient _serviceProviderClient;
    private Task? _ensureInitialServicesRetrievedTask;

    internal RpcServiceProviderClientEndpoint( GlobalServiceProvider serviceProvider, string pipeName ) : base(
        serviceProvider.Underlying,
        pipeName )
    {
        this._serviceProvider = serviceProvider;
        this._serviceProviderClient = new RpcServiceProviderClient( this );
    }

    protected override IEnumerable<RpcClient> CreateServiceClients() => [this._serviceProviderClient];

    protected override Task EnsureInitialServicesRetrievedAsync( CancellationToken cancellationToken )
    {
        if ( this._ensureInitialServicesRetrievedTask == null )
        {
            this._ensureInitialServicesRetrievedTask = this.EnsureInitialServicesRetrievedCoreAsync( cancellationToken );

            return this._ensureInitialServicesRetrievedTask;
        }
        else
        {
            return this._ensureInitialServicesRetrievedTask.WithCancellation( cancellationToken );
        }
    }

    private async Task EnsureInitialServicesRetrievedCoreAsync( CancellationToken cancellationToken )
    {
        // The client must be initialized at this point.
        var api = this._serviceProviderClient.GetApiDangerous();

        var registeredServices = await api.GetRegisteredServicesAsync( cancellationToken )
            .WarnIfLongAsync( this.Logger, nameof(IRpcServiceProviderApi.GetRegisteredServicesAsync), cancellationToken );

        var servicesByPipeName = registeredServices.GroupBy( s => s.PipeName );

        foreach ( var serviceGroup in servicesByPipeName )
        {
            var clients = this.CreateClientsFromServiceInfo( serviceGroup );

            await this.AddServiceClientsAsync( serviceGroup.Key, clients, cancellationToken );
        }
    }

    private ImmutableArray<RpcClient> CreateClientsFromServiceInfo( IEnumerable<RpcServiceInfo> serviceInfos )
    {
        var clients = ImmutableArray.CreateBuilder<RpcClient>();

        foreach ( var serviceInfo in serviceInfos )
        {
            var type = Type.GetType( serviceInfo.FactoryTypeName, true ).AssertNotNull();
            var serviceFactory = (IRpcServiceFactory) Activator.CreateInstance( type ).AssertNotNull();
            var client = serviceFactory.CreateRpcClient( this._serviceProvider, this );
            clients.Add( client );
        }

        return clients.ToImmutable();
    }

    /// <summary>
    /// Aggregates all events raised by any service of the endpoint.
    /// </summary>
    public event Action<RpcEventData>? EventReceived;

    internal async ValueTask<T> GetApiAsync<T>( CancellationToken cancellationToken )
        where T : class, IRpcApi
    {
        var client = await this.GetClientAsync<RpcClient<T>>( cancellationToken );

        return await client.GetApiAsync( cancellationToken );
    }

    internal async ValueTask<T> GetClientAsync<T>( CancellationToken cancellationToken )
        where T : RpcClient
    {
        await this.EnsureInitialServicesRetrievedAsync( cancellationToken );

        return this.GetRequiredClient<T>();
    }

    protected override async Task OnEventReceivedAsync( RpcEventEnvelope envelope, CancellationToken cancellationToken )
    {
        // We call the base first to make sure that the clients process their own messages first.
        await base.OnEventReceivedAsync( envelope, cancellationToken );

        if ( envelope.Data is ServicesAddedEventData servicesAddedEventData )
        {
            this.OnServicesAdded( servicesAddedEventData );
        }

        // Then we republish the message.
        this.Logger.Trace?.Log(
            $"OnEventReceivedAsync: Republishing event {envelope.Data.Category} from {envelope.OriginatingApi} to {this.EventReceived?.GetInvocationList().Length ?? 0} handlers." );

        this.EventReceived?.Invoke( envelope.Data );
    }

    private void OnServicesAdded( ServicesAddedEventData servicesAdded )
    {
        var pipeName = servicesAdded.Services.Select( s => s.PipeName ).Distinct().Single();
        var clients = this.CreateClientsFromServiceInfo( servicesAdded.Services );

        this.ExecuteBackgroundTask( ct => this.AddServiceClientsAsync( pipeName, clients, ct ) );
    }
}