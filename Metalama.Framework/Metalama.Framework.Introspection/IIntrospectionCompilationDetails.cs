// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.Collections.Immutable;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Exposes the compilation results but not the transformed source code.
/// </summary>
[PublicAPI]
public interface IIntrospectionCompilationDetails
{
    /// <summary>
    /// Gets the list of diagnostics reported by Metalama and by aspects.
    /// </summary>
    ImmutableArray<IIntrospectionDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the list of aspect classes in the compilation.
    /// </summary>
    ImmutableArray<IIntrospectionAspectClass> AspectClasses { get; }

    /// <summary>
    /// Gets the ordered list of aspect layers in the compilation. Note that when the current object represents several projects,
    /// the execution order of the aspect layers is not relevant.
    /// </summary>
    ImmutableArray<IIntrospectionAspectLayer> AspectLayers { get; }

    /// <summary>
    /// Gets the list of aspect instances in the compilation.
    /// </summary>
    ImmutableArray<IIntrospectionAspectInstance> AspectInstances { get; }

    /// <summary>
    /// Gets the list of advice in the compilation.
    /// </summary>
    ImmutableArray<IIntrospectionAdvice> Advice { get; }

    /// <summary>
    /// Gets the list of transformations applied to source code.
    /// </summary>
    ImmutableArray<IIntrospectionTransformation> Transformations { get; }

    /// <summary>
    /// Gets a value indicating whether Metalama is enabled on this project.
    /// </summary>
    bool IsMetalamaEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the processing of the compilation by Metalama was successful
    /// for all projects in the current set. This property returns <c>true</c> if the Metalama compilation
    /// process completed successfully, even if it resulted the compilation processes reported errors.
    /// These errors would be visible in the <see cref="Diagnostics"/> collection.
    /// </summary>
    bool HasMetalamaSucceeded { get; }
}