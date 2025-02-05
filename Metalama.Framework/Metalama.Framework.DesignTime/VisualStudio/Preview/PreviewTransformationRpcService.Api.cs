// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.VisualStudio.Preview;

internal sealed partial class PreviewTransformationRpcService
{
    private sealed class Api : IPreviewTransformationRpcApi
    {
        private readonly PreviewTransformationRpcService _parent;

        public Api( PreviewTransformationRpcService parent )
        {
            this._parent = parent;
        }

        public Task<SerializablePreviewTransformationResult> PreviewTransformationAsync(
            ProjectKey projectKey,
            string syntaxTreeName,
            CancellationToken cancellationToken )
        {
            return this._parent._transformationPreviewService.PreviewTransformationAsync( projectKey, syntaxTreeName, cancellationToken );
        }
    }
}