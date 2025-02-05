// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.VisualStudio.Preview;

internal interface IPreviewTransformationRpcApi : IRpcApi
{
    /// <summary>
    /// Computes the transformed code by running the pipeline, and returns the result.
    /// </summary>
    Task<SerializablePreviewTransformationResult> PreviewTransformationAsync(
        ProjectKey projectKey,
        string syntaxTreeName,
        CancellationToken cancellationToken = default );
}