// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.SourceGeneration;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;

/// <summary>
/// Implementation of <see cref="ProjectSourceGenerator"/> in the Visual Studio analysis process. It establishes a connection to the user process
/// to publish generated sources.
/// </summary>
internal sealed class VsAnalysisProcessProjectSourceGenerator : AnalysisProcessProjectSourceGenerator
{
    private readonly SourceGeneratorRpcService? _sourceGeneratorRpcService;

    public VsAnalysisProcessProjectSourceGenerator( GlobalServiceProvider serviceProvider, IProjectOptions projectOptions, ProjectKey projectKey ) : base(
        serviceProvider,
        projectOptions,
        projectKey )
    {
        var endpoint = serviceProvider.GetService<IRpcServiceProviderServerEndpointProvider>()?.Endpoint;

        if ( endpoint != null )
        {
            this._sourceGeneratorRpcService = endpoint.GetRequiredService<SourceGeneratorRpcService>();
            this._sourceGeneratorRpcService.ClientConnected += this.OnClientConnected;
            this.PendingTasks.Run( () => endpoint.RegisterProjectAsync( this.ProjectKey, this.ApplicationExitingToken ) );
        }
        else
        {
            this.Logger.Warning?.Log( "The project handler was created without an endpoint." );
        }
    }

    private void OnClientConnected( ProjectKey projectKey )
    {
        if ( projectKey == this.ProjectKey )
        {
            // When a client connects, we update the touch file to force that client to request the info again.
            this.UpdateTouchFile();
        }
    }

    protected override Task PublishGeneratedSourcesAsync( ProjectKey projectKey, CancellationToken cancellationToken )
    {
        if ( this._sourceGeneratorRpcService == null )
        {
            this.Logger.Warning?.Log( $"Do not publish the generated source for '{projectKey}' because there is no endpoint." );

            return Task.CompletedTask;
        }

        if ( this.LastSourceGeneratorResult == null )
        {
            this.Logger.Warning?.Log( $"Do not publish the generated source for '{projectKey}' because there is none." );

            return Task.CompletedTask;
        }
        else
        {
            this.Logger.Trace?.Log( $"Publishing generated source of '{projectKey}' to the user process." );

            var generatedSources = this.LastSourceGeneratorResult.AdditionalSources
                .ToImmutableDictionary( x => x.Key, x => x.Value.GeneratedSyntaxTree.ToString() );

            return this._sourceGeneratorRpcService.PublishGeneratedSourcesAsync(
                projectKey,
                generatedSources,
                cancellationToken );
        }
    }
}