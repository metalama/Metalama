// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.SourceGeneration;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;

/// <summary>
/// Implementation of <see cref="ProjectSourceGenerator"/> in the Visual Studio analysis process. It establishes a connection to the user process
/// to publish generated sources.
/// </summary>
internal sealed class VsAnalysisProcessProjectSourceGenerator : AnalysisProcessProjectSourceGenerator
{
    private readonly RpcServiceProviderServerEndpoint? _serviceProviderEndpoint;
    private readonly Task _initializationTask;

    private SourceGeneratorRpcService? _sourceGeneratorRpcService;

    public VsAnalysisProcessProjectSourceGenerator( GlobalServiceProvider serviceProvider, IProjectOptions projectOptions, ProjectKey projectKey ) : base(
        serviceProvider,
        projectOptions,
        projectKey )
    {
        this._serviceProviderEndpoint = serviceProvider.GetService<IRpcServiceProviderServerEndpointProvider>()?.Endpoint;

        if ( this._serviceProviderEndpoint != null )
        {
            this._initializationTask = this.InitializeAsync();
        }
        else
        {
            this._initializationTask = Task.CompletedTask;
            this.Logger.Warning?.Log( "The project handler was created without an endpoint." );
        }
    }

    private async Task InitializeAsync()
    {
        this._sourceGeneratorRpcService =
            await this._serviceProviderEndpoint!.GetRequiredServiceAsync<SourceGeneratorRpcService>( this.ApplicationExitingToken );

        this._sourceGeneratorRpcService.ClientConnected += this.OnClientConnected;
        this.PendingTasks.Enqueue( () => this._serviceProviderEndpoint!.RegisterProjectAsync( this.ProjectKey, this.ApplicationExitingToken ) );
    }

    private void OnClientConnected( ProjectKey projectKey )
    {
        if ( projectKey == this.ProjectKey )
        {
            // When a client connects, we update the touch file to force that client to request the info again.
            this.UpdateTouchFile();
        }
    }

    protected override async Task PublishGeneratedSourcesAsync( ProjectKey projectKey, CancellationToken cancellationToken )
    {
        await this._initializationTask.WithCancellation( cancellationToken ).WarnIfLongAsync( this.Logger, nameof(this.InitializeAsync), cancellationToken );

        if ( this._sourceGeneratorRpcService == null )
        {
            this.Logger.Warning?.Log( $"Do not publish the generated source for '{projectKey}' because there is no endpoint." );

            return;
        }

        if ( this.LastSourceGeneratorResult == null )
        {
            this.Logger.Warning?.Log( $"Do not publish the generated source for '{projectKey}' because there is none." );

            return;
        }

        this.Logger.Trace?.Log( $"Publishing generated source of '{projectKey}' to the user process." );

        var generatedSources = this.LastSourceGeneratorResult.AdditionalSources
            .ToImmutableDictionary( x => x.Key, x => x.Value.GeneratedSyntaxTree.ToString() );

        await this._sourceGeneratorRpcService.PublishGeneratedSourcesAsync(
            projectKey,
            generatedSources,
            cancellationToken );
    }
}