// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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