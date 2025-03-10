// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// Represents an aspect layer within an aspect class isntance.
/// </summary>
internal sealed class AspectLayerInstance
{
    public AspectLayerInstance( IAspectInstanceInternal aspectInstance, string? layerName, CompilationModel initialCompilation )
    {
        this.AspectInstance = aspectInstance;
        this.InitialCompilation = initialCompilation;
        this.AspectLayerId = new AspectLayerId( aspectInstance.AspectClass, layerName );
    }

    private AspectLayerInstance( CompilationModel initialCompilation )
    {
        this.InitialCompilation = initialCompilation;
        this.AspectInstance = null!;
        this.AspectLayerId = default;
    }

    public static AspectLayerInstance CreateTestInstance( CompilationModel initialCompilation )
    {
        return new AspectLayerInstance( initialCompilation );
    }

    public IAspectInstanceInternal AspectInstance { get; }

    public AspectLayerId AspectLayerId { get; }

    /// <summary>
    /// Gets the immutable <see cref="CompilationModel"/> <i>before</i> the execution of the layer.
    /// </summary>
    public CompilationModel InitialCompilation { get; }
}