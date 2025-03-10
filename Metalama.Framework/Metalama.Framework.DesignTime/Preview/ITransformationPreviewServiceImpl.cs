// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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