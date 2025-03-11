// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.SourceGeneration;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;

/// <summary>
/// Implementation of <see cref="ProjectSourceGenerator"/> in the Visual Studio user process. It receives generated source code
/// from the analysis process.
/// </summary>
internal sealed class VsUserProcessProjectSourceGenerator : ProjectSourceGenerator
{
    private readonly ServiceHubRpcService _serviceHub;
    private readonly IProjectSourceGeneratorObserver? _observer;
    private readonly DesignTimeExceptionHandler? _exceptionHandler;

    private ImmutableDictionary<string, string>? _sources;

    public VsUserProcessProjectSourceGenerator( GlobalServiceProvider serviceProvider, IProjectOptions projectOptions, ProjectKey projectKey ) : base(
        serviceProvider,
        projectOptions,
        projectKey )
    {
        this._serviceHub = serviceProvider.GetRequiredService<IServiceHubRpcServiceProvider>().ServiceHub;
        this._observer = serviceProvider.GetService<IProjectSourceGeneratorObserver>();
        this._serviceHub.EventReceived += this.OnServiceHubEventReceived;
        this._exceptionHandler = serviceProvider.GetService<DesignTimeExceptionHandler>();

        _ = this.InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var client = await this._serviceHub.GetClientForProjectAsync<SourceGeneratorRpcClient>(
                this.ProjectKey,
                CancellationToken.None );

            var api = await client.GetApiAsync( CancellationToken.None );
            await api.RegisterAsync( this.ProjectKey, CancellationToken.None );
        }
        catch ( Exception ex )
        {
            this._exceptionHandler?.ReportException( ex, this.Logger );
        }
    }

    private void OnServiceHubEventReceived( RpcEventData eventData )
    {
        if ( eventData.Category != nameof(ISourceGeneratorRpcApi) )
        {
            return;
        }

        var sourceGeneratorEvent = (GeneratedSourceChangedEventData) eventData;

        if ( sourceGeneratorEvent.ProjectKey != this.ProjectKey )
        {
            return;
        }

        this._sources = sourceGeneratorEvent.Sources;
        this._observer?.OnGeneratedCodePublished( this.ProjectKey, this._sources );
    }

    public override SourceGeneratorResult GenerateSources( Compilation compilation, TestableCancellationToken cancellationToken )
    {
        try
        {
            if ( this._sources == null )
            {
                // If we have not received the source yet, see if it was received by the client before we were created.
                if ( this.TryGetGeneratedSourcesIfAvailable( this.ProjectKey, out var sources ) )
                {
                    this.Logger.Trace?.Log( $"Generated sources for '{this.ProjectKey}' were retrieved from ServiceClient." );
                    this._sources = sources;
                }
                else
                {
                    this.Logger.Warning?.Log( $"Information about generated sources for '{this.ProjectKey}' is not available." );

                    // Retrieve in the background.
                    _ = this.RetrieveGeneratedSourcesAsync( this.ProjectKey );

                    return SourceGeneratorResult.Empty;
                }
            }

            return new TextSourceGeneratorResult( this._sources.AssertNotNull() );
        }
        catch ( Exception e )
        {
            // This is our entry point to Roslyn. Make sure we don't propagate exceptions.
            this._exceptionHandler?.ReportException( e, this.Logger );

            return SourceGeneratorResult.Empty;
        }
    }

    private bool TryGetGeneratedSourcesIfAvailable( ProjectKey projectKey, out ImmutableDictionary<string, string>? sources )
    {
        var clientTask = this._serviceHub.GetClientForProjectAsync<SourceGeneratorRpcClient>(
            projectKey,
            CancellationToken.None );

        if ( !clientTask.IsCompleted )
        {
            this.Logger.Warning?.Log( $"TryGetGenerateSourcesIfAvailable('{projectKey}'): endpoint not ready." );
            sources = null;

            return false;
        }

#pragma warning disable VSTHRD002
        if ( !clientTask.Result.TryGetCachedGeneratedSources( projectKey, out sources ) )
#pragma warning restore VSTHRD002
        {
            this.Logger.Warning?.Log( $"TryGetGenerateSourcesIfAvailable('{projectKey}'): no result is available in cache." );
            sources = null;

            return false;
        }

        return true;
    }

    private async Task RetrieveGeneratedSourcesAsync( ProjectKey projectKey )
    {
        var client = await this._serviceHub.GetClientForProjectAsync<SourceGeneratorRpcClient>(
            projectKey,
            CancellationToken.None );

        await client.GetGeneratedSourcesAsync( projectKey, CancellationToken.None );
    }
}