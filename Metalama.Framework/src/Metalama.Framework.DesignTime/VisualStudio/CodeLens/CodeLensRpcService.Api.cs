// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.DesignTime.CodeLens;
using Metalama.Framework.DesignTime.Contracts.CodeLens;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Utilities.Threading;

namespace Metalama.Framework.DesignTime.VisualStudio.CodeLens;

internal partial class CodeLensRpcService
{
    private sealed class Api : ICodeLensRpcApi
    {
        private readonly CodeLensRpcService _parent;

        public Api( CodeLensRpcService parent )
        {
            this._parent = parent;
        }

        public Task<CodeLensSummary> GetCodeLensSummaryAsync(
            ProjectKey projectKey,
            SerializableDeclarationId symbolId,
            CancellationToken cancellationToken = default )
        {
            return this._parent._codeLensServiceImpl.GetCodeLensSummaryAsync( projectKey, symbolId, cancellationToken.ToTestable() );
        }

        public Task<ICodeLensDetailsTable> GetCodeLensDetailsAsync(
            ProjectKey projectKey,
            SerializableDeclarationId symbolId,
            CancellationToken cancellationToken = default )
        {
            return this._parent._codeLensServiceImpl.GetCodeLensDetailsAsync( projectKey, symbolId, cancellationToken.ToTestable() );
        }
    }
}