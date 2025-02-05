// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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