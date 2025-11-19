// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;

/// <summary>
/// Implements the client endpoint for <see cref="IRpcServiceProviderApi"/>. Runs in the user process.
/// </summary>
public sealed class RpcServiceProviderClientEndpoint : ClientEndpoint
{
    private readonly GlobalServiceProvider _serviceProvider;
    private readonly RpcServiceProviderClient _serviceProviderClient;
    private readonly DesignTimeExtensionManager? _extensionManager;
    private readonly object _initializeSync = new();

    private Task? _ensureInitialServicesRetrievedTask;

    internal RpcServiceProviderClientEndpoint( GlobalServiceProvider serviceProvider, string pipeName ) : base(
        serviceProvider.Underlying,
        pipeName )
    {
        this._serviceProvider = serviceProvider;
        this._extensionManager = serviceProvider.GetService<DesignTimeExtensionManager>();
        this._serviceProviderClient = new RpcServiceProviderClient( this );
    }

    protected override IEnumerable<RpcClient> CreateServiceClients() => [this._serviceProviderClient];

    protected override Task EnsureInitialServicesRetrievedAsync( CancellationToken cancellationToken )
    {
        lock ( this._initializeSync )
        {
            if ( this._ensureInitialServicesRetrievedTask == null )
            {
                this._ensureInitialServicesRetrievedTask = this.EnsureInitialServicesRetrievedCoreAsync( cancellationToken );

                return this._ensureInitialServicesRetrievedTask;
            }
        }

        return this._ensureInitialServicesRetrievedTask.WithCancellation( cancellationToken );
    }

    private async Task EnsureInitialServicesRetrievedCoreAsync( CancellationToken cancellationToken )
    {
        var api = await this._serviceProviderClient.GetApiAsync( cancellationToken );

        var registeredServices = await api.GetRegisteredServicesAsync( cancellationToken )
            .WarnIfLongAsync( this.Logger, nameof(IRpcServiceProviderApi.GetRegisteredServicesAsync), cancellationToken );

        var servicesByPipeName = registeredServices.GroupBy( s => s.PipeName );

        await Task.WhenAll(
            servicesByPipeName.Select(
                async serviceGroup =>
                {
                    var clients = await this.CreateClientsFromServiceInfoAsync( serviceGroup, cancellationToken );

                    await this.AddServiceClientsAsync( serviceGroup.Key, clients, cancellationToken );
                } ) );
    }

    private async Task<ImmutableArray<RpcClient>> CreateClientsFromServiceInfoAsync( IEnumerable<RpcServiceInfo> serviceInfos, CancellationToken cancellationToken )
    {
        var clients = await Task.WhenAll(
            serviceInfos.Select(
                async serviceInfo =>
                {
                    if ( serviceInfo.ExtensionName != null )
                    {
                        if ( this._extensionManager == null )
                        {
                            throw new InvalidOperationException( $"{nameof(DesignTimeExtensionManager)} is not available." );
                        }

                        await this._extensionManager.GetExtensionAsync( serviceInfo.ExtensionName, cancellationToken )
                            .WarnIfLongAsync( this.Logger, $"Awaiting for extension '{serviceInfo.ExtensionName}'.", cancellationToken );
                    }

                    var type = Type.GetType( serviceInfo.FactoryTypeName, true ).AssertNotNull();
                    var serviceFactory = (IRpcServiceFactory) Activator.CreateInstance( type ).AssertNotNull();

                    return serviceFactory.CreateRpcClient( this._serviceProvider, this );
                } ) );
        
        return clients.ToImmutableArray();
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

        return await this.GetOrWaitForClientAsync<T>( cancellationToken );
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
            $"OnEventReceivedAsync: Republishing event {envelope.Data.GetType().Name} from {envelope.OriginatingApi} to {this.EventReceived?.GetInvocationList().Length ?? 0} handlers." );

        this.EventReceived?.Invoke( envelope.Data );
    }

    private void OnServicesAdded( ServicesAddedEventData servicesAdded )
    {
        var pipeName = servicesAdded.Services.Select( s => s.PipeName ).Distinct().Single();

        foreach ( var group in servicesAdded.Services.GroupBy( s => s.ExtensionName ) )
        {
            this.ExecuteBackgroundTask(
                ct => this.AddServiceClientGroupAsync( pipeName, group.Key, group.ToList(), ct ),
                nameof(this.AddServiceClientGroupAsync) );
        }
    }

    private async Task AddServiceClientGroupAsync(
        string pipeName,
        string? extensionName,
        IReadOnlyCollection<RpcServiceInfo> services,
        CancellationToken cancellationToken )
    {
        this.Logger.Trace?.Log( $"Registering services {string.Join( "; ", services.Select( x => x.FactoryTypeName ) )} from extension '{extensionName}'." );

        if ( extensionName != null )
        {
            if ( this._extensionManager == null )
            {
                throw new InvalidOperationException( $"There is no {nameof(DesignTimeExtensionManager)}." );
            }

            // Wait until the extension is registered.
            var getExtensionTask = this._extensionManager.GetExtensionAsync( extensionName, cancellationToken );

            if ( !getExtensionTask.IsCompleted )
            {
                this.Logger.Trace?.Log( $"Waiting for extension '{extensionName}'." );

                await getExtensionTask
                    .WarnIfLongAsync( this.Logger, $"Waiting for extension {extensionName} to be registered.", cancellationToken );
            }

            this.Logger.Trace?.Log( $"The extension '{extensionName}' is available." );
        }

        var clients = await this.CreateClientsFromServiceInfoAsync( services, cancellationToken );

        await this.AddServiceClientsAsync( pipeName, clients, cancellationToken );
    }
}