// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Transformations;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Input of the aspect linker.
/// </summary>
internal readonly struct AspectLinkerInput
{
    /// <summary>
    /// Gets the input compilation model, modified by all aspects.
    /// </summary>
    public CompilationModel FinalCompilationModel { get; }

    public CompilationModel InitialCompilationModel { get; }

    /// <summary>
    /// Gets a list of non-observable transformations.
    /// </summary>
    public IReadOnlyCollection<ITransformation> Transformations { get; }

    /// <summary>
    /// Gets a list of ordered aspect layers.
    /// </summary>
    public IReadOnlyList<OrderedAspectLayer> OrderedAspectLayers { get; }
    
    public AspectLinkerInput(
        CompilationModel initialCompilationModel,
        CompilationModel finalCompilationModel,
        IReadOnlyCollection<ITransformation> transformations,
        IReadOnlyList<OrderedAspectLayer> orderedAspectLayers )
    {
        this.InitialCompilationModel = initialCompilationModel;
        this.FinalCompilationModel = finalCompilationModel;
        this.Transformations = transformations;
        this.OrderedAspectLayers = orderedAspectLayers;
    }
}