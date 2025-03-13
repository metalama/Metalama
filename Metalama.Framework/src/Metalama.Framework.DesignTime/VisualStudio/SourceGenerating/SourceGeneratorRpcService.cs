// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Services;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;

internal sealed partial class SourceGeneratorRpcService : RpcService<ISourceGeneratorRpcApi>
{
    private readonly ConcurrentDictionary<ProjectKey, ImmutableDictionary<string, string>> _generatedSourcesCache = new();
    private readonly ILogger _logger;

    public SourceGeneratorRpcService( GlobalServiceProvider serviceProvider, ServerEndpoint serverEndpoint ) : base( serverEndpoint )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(SourceGeneratorRpcService) );
    }

    public async Task PublishGeneratedSourcesAsync(
        ProjectKey projectKey,
        ImmutableDictionary<string, string> generatedSources,
        CancellationToken cancellationToken = default )
    {
        this._logger.Trace?.Log( $"Publishing source for the client '{projectKey}'." );

        this._generatedSourcesCache.AddOrUpdate( projectKey, generatedSources, ( _, _ ) => generatedSources );

        await this.RaiseEventAsync( new GeneratedSourceChangedEventData( projectKey, generatedSources ), cancellationToken );
    }

    protected override ISourceGeneratorRpcApi CreateApi( IRpcEventSender eventSender ) => new Api( this );

    public event Action<ProjectKey>? ClientConnected;
}