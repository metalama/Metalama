// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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