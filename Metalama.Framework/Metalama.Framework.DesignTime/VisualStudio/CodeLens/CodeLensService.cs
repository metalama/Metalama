// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

    public CodeLensService( GlobalServiceProvider serviceProvider )
    {
        this._serviceHub = serviceProvider.GetRequiredService<IServiceHubRpcServiceProvider>().ServiceHub;
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