// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.DesignTime.Contracts.CodeLens;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.CodeLens;

/// <summary>
/// Cross-process interface equivalent to <see cref="ICodeLensService"/>.
/// </summary>
internal interface ICodeLensServiceImpl : IGlobalService
{
    Task<CodeLensSummary> GetCodeLensSummaryAsync( ProjectKey projectKey, SerializableDeclarationId symbolId, TestableCancellationToken cancellationToken );

    Task<ICodeLensDetailsTable> GetCodeLensDetailsAsync(
        ProjectKey projectKey,
        SerializableDeclarationId symbolId,
        TestableCancellationToken cancellationToken );
}