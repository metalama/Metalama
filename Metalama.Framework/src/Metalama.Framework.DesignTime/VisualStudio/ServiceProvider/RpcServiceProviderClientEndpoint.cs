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
    private readonly ITestSynchronizationProvider? _testSyncProvider;
    private readonly HashSet<string> _syncedExtensions = new();
    private readonly object _initializeSync = new();

    private Task? _ensureInitialServicesRetrievedTask;

    internal RpcServiceProviderClientEndpoint( GlobalServiceProvider serviceProvider, string pipeName ) : base(
        serviceProvider.Underlying,
        pipeName )
    {
        this._serviceProvider = serviceProvider;
        this._extensionManager = serviceProvider.GetService<DesignTimeExtensionManager>();
        this._testSyncProvider = serviceProvider.Underlying.GetService( typeof(ITestSynchronizationProvider) ) as ITestSynchronizationProvider;
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

        await Task.WhenAll( servicesByPipeName.Select( serviceGroup => this.AddServiceClientsAsync( serviceGroup.Key, serviceGroup, cancellationToken ) ) );
    }

    private async Task AddServiceClientsAsync( string pipeName, IEnumerable<RpcServiceInfo> serviceInfos, CancellationToken cancellationToken )
    {
        var clients = new List<RpcClient>();

        foreach ( var group in serviceInfos.GroupBy( serviceInfo => serviceInfo.ExtensionName ) )
        {
            var extensionName = group.Key;

            if ( extensionName != null )
            {
                if ( this._extensionManager == null )
                {
                    throw new InvalidOperationException( $"{nameof(DesignTimeExtensionManager)} is not available." );
                }

                // Test synchronization point: allows tests to control timing of the IsCompleted check.
                // Only hit the sync point once per extension to avoid blocking on retry.
                if ( this._testSyncProvider != null && this._syncedExtensions.Add( extensionName ) )
                {
                    await this._testSyncProvider.SyncPointAsync(
                        $"RpcServiceProviderClientEndpoint.BeforeExtensionCheck:{extensionName}",
                        cancellationToken );
                }

                var getExtensionTask = this._extensionManager.GetExtensionAsync( extensionName, CancellationToken.None );

                if ( !getExtensionTask.IsCompleted )
                {
                    // If the extension is not loaded yet, schedule a continuation task to load the services
                    // when it will be available. We do not await for it now because it would make other services
                    // dependent upon the availability of this extension,

                    var extensionServiceInfos = group.ToList();

                    this.Logger.Trace?.Log(
                        $"Cannot add RPC services {string.Join( ", ", extensionServiceInfos )} because the extension '{extensionName}' is not loaded yet." );

                    this.ExecuteBackgroundTask(
                        async ct =>
                        {
                            await this._extensionManager.GetExtensionAsync( extensionName, ct );

                            this.Logger.Trace?.Log( $"Loading RPC services for extension '{extensionName}'." );

                            await this.AddServiceClientsAsync( pipeName, extensionServiceInfos, this.DisposeCancellationToken );
                        },
                        $"Loading RPC services for extension '{extensionName}'." );

                    continue;
                }
            }

            foreach ( var serviceInfo in group )
            {
                var type = Type.GetType( serviceInfo.FactoryTypeName, true ).AssertNotNull();
                var serviceFactory = (IRpcServiceFactory) Activator.CreateInstance( type ).AssertNotNull();

                clients.Add( serviceFactory.CreateRpcClient( this._serviceProvider, this ) );
            }
        }

        if ( clients.Count > 0 )
        {
            await this.AddServiceClientsAsync( pipeName, clients.ToImmutableArray(), cancellationToken );
        }
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

        this.ExecuteBackgroundTask(
            ct => this.AddServiceClientsAsync( pipeName, servicesAdded.Services, ct ),
            $"Loading RPC services {string.Join( ", ", servicesAdded.Services )}." );
    }
}