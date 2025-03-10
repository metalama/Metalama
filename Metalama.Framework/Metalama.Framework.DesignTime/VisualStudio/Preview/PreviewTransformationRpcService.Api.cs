// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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