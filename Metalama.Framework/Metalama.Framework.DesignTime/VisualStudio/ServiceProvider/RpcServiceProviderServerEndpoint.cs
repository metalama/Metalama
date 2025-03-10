// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.AspectExplorer;
using Metalama.Framework.DesignTime.VisualStudio.CodeLens;
using Metalama.Framework.DesignTime.VisualStudio.CompilationResultObserver;
using Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus;
using Metalama.Framework.DesignTime.VisualStudio.Preview;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;

/// <summary>
/// Implements the server endpoint for <see cref="IRpcServiceProviderApi"/>. Runs in the analysis process.
/// </summary>
public sealed class RpcServiceProviderServerEndpoint : ServerEndpoint, IGlobalService
{
    // List of services that are loaded by default.
    // This list can be overridden in the constructor for lighter testing scenarios. 
    private static readonly IRpcServiceFactory[] _defaultServiceFactories =
    [
        new CompilationResultObserverRpcServiceFactory(),
        new CompileTimeCodeEditingStatusRpcServiceFactory(),
        new PreviewTransformationRpcServiceFactory(),
        new SourceGeneratorRpcServiceFactory(),
        new CodeLensRpcServiceFactory(),
        new AspectExplorerRpcServiceFactory()
    ];

    private readonly GlobalServiceProvider _serviceProvider;
    private ImmutableArray<IRpcServiceFactory> _serviceFactories;
    private static readonly object _initializeLock = new();
    private static RpcServiceProviderServerEndpoint? _instance;
    private readonly ServiceHubRpcClient? _serviceHubApiClient;
    private readonly RpcServiceProviderService _rpcServiceProviderService;
    private readonly object _addServiceLock = new();

    private bool _isHubRegistrationProcessed;

    /// <summary>
    /// Initializes the global instance of the service.
    /// </summary>
    internal static RpcServiceProviderServerEndpoint GetInstance( GlobalServiceProvider serviceProvider )
    {
        if ( _instance == null )
        {
            lock ( _initializeLock )
            {
                if ( _instance == null )
                {
                    var pipeName = PipeNameProvider.GetPipeName( EndpointRole.Service );
                    _instance = new RpcServiceProviderServerEndpoint( serviceProvider, pipeName, _defaultServiceFactories );
                    _instance.Start();
                }
            }
        }

        return _instance;
    }

    internal RpcServiceProviderServerEndpoint( GlobalServiceProvider serviceProvider, string pipeName, IRpcServiceFactory[]? serviceFactories = null ) : base(
        serviceProvider.Underlying,
        pipeName )
    {
        this._serviceProvider = serviceProvider;
        this._serviceFactories = (serviceFactories ?? _defaultServiceFactories).ToImmutableArray();
        this._rpcServiceProviderService = new RpcServiceProviderService( this, this._serviceFactories.Select( x => x.GetType() ) );

        // This service is optional because some tests don't supply it.
        this._serviceHubApiClient = serviceProvider.GetService<IServiceHubRpcApiClientProvider>()?.ServiceHubApiClient;
    }

    protected override IEnumerable<RpcService> CreateServices()
    {
        return this._serviceFactories.Select( f => f.CreateRpcService( this._serviceProvider, this ) ).Concat( this._rpcServiceProviderService );
    }

    protected override async Task OnServerPipeCreatedAsync( CancellationToken cancellationToken )
    {
        // We must connect to the service hub here and now, otherwise the caller would wait forever for a client.

        if ( this._isHubRegistrationProcessed )
        {
            this.Logger.Trace?.Log( $"Registering '{this.PipeName}' to the hub has already been done." );

            return;
        }

        this._isHubRegistrationProcessed = true;

        if ( this._serviceHubApiClient != null )
        {
            this.Logger.Trace?.Log( $"Registering the endpoint '{this.PipeName}' on the hub." );
            var registrationService = await this._serviceHubApiClient.GetApiAsync( cancellationToken );
            await registrationService.RegisterAnalysisServiceAsync( this.PipeName, cancellationToken );
            this.Logger.Trace?.Log( $"Registering the endpoint '{this.PipeName}' on the hub: completed." );
        }
    }

    public async Task RegisterProjectAsync( ProjectKey projectKey, CancellationToken cancellationToken )
    {
        await this.WaitUntilInitializedAsync( cancellationToken );

        if ( this._serviceHubApiClient != null )
        {
            this.Logger.Trace?.Log( $"Registering the project '{projectKey}' on the hub." );
            var registrationService = await this._serviceHubApiClient.GetApiAsync( cancellationToken );
            await registrationService.RegisterAnalysisServiceProjectAsync( projectKey, this.PipeName, cancellationToken );
        }
    }

    internal void AddServices( IReadOnlyCollection<IRpcServiceFactory> serviceFactories )
    {
        List<IRpcServiceFactory> newServiceFactories;
        ImmutableArray<RpcService> services;

#if DEBUG
        var duplicates =
            this._serviceFactories.Concat( serviceFactories )
                .GroupBy( f => f.GetType() )
                .Where( g => g.Count() > 1 )
                .Select( g => g.Key.Name )
                .ToList();

        if ( duplicates.Count > 0 )
        {
            throw new ArgumentOutOfRangeException(
                nameof(serviceFactories),
                $"There would be more than one service factory of type {string.Join( ", ", duplicates )}" );
        }
#endif

        lock ( this._addServiceLock )
        {
            newServiceFactories = serviceFactories.Except( this._serviceFactories ).ToList();

            this._serviceFactories = this._serviceFactories.AddRange( newServiceFactories );

            services = newServiceFactories
                .Select( f => f.CreateRpcService( this._serviceProvider, this ) )
                .ToImmutableArray();
        }

        var pipeName = this.AddServices( services );

        this.ExecuteBackgroundTask( ct => this._rpcServiceProviderService.AddServicesAsync( pipeName, newServiceFactories, ct ) );
    }
}