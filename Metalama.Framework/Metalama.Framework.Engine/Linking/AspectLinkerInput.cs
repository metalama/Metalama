// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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