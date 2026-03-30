// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.CodeLens;
using Metalama.Framework.DesignTime.Contracts.CodeLens;
using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.VisualStudio.CodeLens;

internal sealed class CodeLensService : ICodeLensService
{
    private readonly ServiceHubRpcService _serviceHub;
    private readonly ILogger _logger;

    public CodeLensService( GlobalServiceProvider serviceProvider )
    {
        this._serviceHub = serviceProvider.GetRequiredService<IServiceHubRpcServiceProvider>().ServiceHub;
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(CodeLensService) );
    }

    public async Task GetCodeLensSummaryAsync(
        Compilation compilation,
        ISymbol symbol,
        ICodeLensSummary?[] result,
        CancellationToken cancellationToken = default )
    {
        var projectKey = compilation.GetProjectKey();

        if ( !projectKey.IsMetalamaEnabled )
        {
            this._logger.Trace?.Log( $"Returning NoAspect because '{compilation.AssemblyName}' is not a Metalama project." );
            result[0] = CodeLensSummary.NoAspect;

            return;
        }

        var analysisProcessApi = await this._serviceHub.GetApiForProjectAsync<ICodeLensRpcApi>(
            projectKey,
            cancellationToken );

        result[0] = await analysisProcessApi.GetCodeLensSummaryAsync( projectKey, symbol.GetSerializableId(), cancellationToken );
    }

    public async Task GetCodeLensDetailsAsync(
        Compilation compilation,
        ISymbol symbol,
        ICodeLensDetails?[] result,
        CancellationToken cancellationToken = default )
    {
        var projectKey = compilation.GetProjectKey();

        if ( !projectKey.IsMetalamaEnabled )
        {
            result[0] = CodeLensDetailsTable.Empty;

            return;
        }

        var analysisProcessApi = await this._serviceHub.GetApiForProjectAsync<ICodeLensRpcApi>(
            projectKey,
            cancellationToken );

        result[0] = await analysisProcessApi.GetCodeLensDetailsAsync( projectKey, symbol.GetSerializableId(), cancellationToken );
    }
}