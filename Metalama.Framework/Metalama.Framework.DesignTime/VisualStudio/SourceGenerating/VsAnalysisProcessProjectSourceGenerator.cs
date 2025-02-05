// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
        var endpoint = serviceProvider.GetService<RpcServiceProviderServerEndpoint>();

        if ( endpoint != null )
        {
            this._sourceGeneratorRpcService = endpoint.GetRequiredService<SourceGeneratorRpcService>();
            this._sourceGeneratorRpcService.ClientConnected += this.OnClientConnected;
            this.PendingTasks.Run( () => endpoint.RegisterProjectAsync( this.ProjectKey, CancellationToken.None ) );
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