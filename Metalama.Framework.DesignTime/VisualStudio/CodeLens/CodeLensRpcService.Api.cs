// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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