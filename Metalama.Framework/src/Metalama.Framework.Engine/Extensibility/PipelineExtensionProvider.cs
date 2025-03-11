// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// A service that exposes the collection of <see cref="PipelineExtension"/> in the current project.
/// </summary>
internal sealed class PipelineExtensionProvider : IProjectService
{
    public ImmutableArray<PipelineExtension> Extensions { get; }

    internal PipelineExtensionProvider( ImmutableArray<PipelineExtension> extensions )
    {
        this.Extensions = extensions;
    }
}