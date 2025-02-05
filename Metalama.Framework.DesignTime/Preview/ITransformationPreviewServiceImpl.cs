// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.Preview;

internal interface ITransformationPreviewServiceImpl : IGlobalService
{
    /// <param name="projectKey">Key for the project that contains the file that is being previewed.</param>
    /// <param name="syntaxTreeName">Path for the syntax tree that is being previewed. This file may or may not exist in the original project.</param>
    Task<SerializablePreviewTransformationResult> PreviewTransformationAsync(
        ProjectKey projectKey,
        string syntaxTreeName,
        CancellationToken cancellationToken );
}