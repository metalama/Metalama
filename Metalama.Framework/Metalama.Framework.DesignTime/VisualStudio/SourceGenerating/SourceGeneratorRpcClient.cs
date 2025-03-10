// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Services;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;

internal sealed class SourceGeneratorRpcClient : RpcClient<ISourceGeneratorRpcApi>
{
    private readonly ConcurrentDictionary<ProjectKey, ImmutableDictionary<string, string>> _cachedGeneratedSources = new();
    private readonly ILogger _logger;

    public SourceGeneratorRpcClient( GlobalServiceProvider serviceProvider, ClientEndpoint endpoint ) : base( endpoint )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(SourceGeneratorRpcClient) );
    }

    public async ValueTask<ImmutableDictionary<string, string>> GetGeneratedSourcesAsync( ProjectKey projectKey, CancellationToken cancellationToken )
    {
        if ( this.TryGetCachedGeneratedSources( projectKey, out var generatedSources ) )
        {
            return generatedSources;
        }
        else
        {
            var api = await this.GetApiAsync( cancellationToken );
            generatedSources = await api.GetGeneratedSourcesAsync( projectKey, cancellationToken );
            this._cachedGeneratedSources.AddOrUpdate( projectKey, generatedSources, ( _, _ ) => generatedSources );

            return generatedSources;
        }
    }

    public bool TryGetCachedGeneratedSources( ProjectKey projectKey, [NotNullWhen( true )] out ImmutableDictionary<string, string>? sources )
    {
        if ( this._cachedGeneratedSources.TryGetValue( projectKey, out sources ) )
        {
            this._logger.Trace?.Log( $"Found cached generated sources for '{projectKey}'." );

            return true;
        }
        else
        {
            this._logger.Trace?.Log( $"No cached generated sources for '{projectKey}'." );

            return false;
        }
    }

    protected override async Task OnEventReceivedAsync( RpcEventData eventData, CancellationToken cancellationToken )
    {
        if ( eventData is GeneratedSourceChangedEventData sourceEventData )
        {
            this._logger.Trace?.Log( $"Received new generated code from the remote host for project '{sourceEventData.ProjectKey}'." );

            // Store the event so that a source generator that would be create later can retrieve it.
            this._cachedGeneratedSources[sourceEventData.ProjectKey] = sourceEventData.Sources;
        }

        await base.OnEventReceivedAsync( eventData, cancellationToken );
    }
}