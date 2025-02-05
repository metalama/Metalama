// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.DesignTime.CodeLens;
using Metalama.Framework.DesignTime.Contracts.CodeLens;
using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.VisualStudio.CodeLens;

internal interface ICodeLensRpcApi : IRpcApi
{
    /// <summary>
    /// Gets the inline summary code lens text for a symbol.
    /// </summary>
    Task<CodeLensSummary> GetCodeLensSummaryAsync(
        ProjectKey projectKey,
        SerializableDeclarationId symbolId,
        [UsedImplicitly] CancellationToken cancellationToken = default );

    /// <summary>
    /// Gets the detailed code lens content that appears when the user clicks on the summary text.
    /// </summary>
    Task<ICodeLensDetailsTable> GetCodeLensDetailsAsync(
        ProjectKey projectKey,
        SerializableDeclarationId symbolId,
        [UsedImplicitly] CancellationToken cancellationToken = default );
}