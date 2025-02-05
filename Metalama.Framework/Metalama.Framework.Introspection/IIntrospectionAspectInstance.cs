// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System.Collections.Immutable;

namespace Metalama.Framework.Introspection;

/// <summary>
///  Represents an instance of an aspect, as well as the results of the aspect instance.
/// </summary>
[PublicAPI]
public interface IIntrospectionAspectInstance : IIntrospectionAspectPredecessor
{
    /// <summary>
    /// Gets the list of diagnostics produced by the aspect.
    /// </summary>
    ImmutableArray<IIntrospectionDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the advice added by the aspect.
    /// </summary>
    IReadOnlyList<IIntrospectionAdvice> Advice { get; }

    /// <summary>
    /// Gets the aspect instance.
    /// </summary>
    IAspect Aspect { get; }

    /// <summary>
    /// Gets the aspect type.
    /// </summary>
    IIntrospectionAspectClass AspectClass { get; }

    /// <summary>
    /// Gets a value indicating whether the current aspect instance has been skipped. This value is <c>true</c> if
    /// the aspect evaluation resulted in an error or if the <see cref="IAspect{T}.BuildAspect"/> method invoked
    /// <see cref="IAspectBuilder.SkipAspect"/>.
    /// </summary>
    bool IsSkipped { get; }

    /// <summary>
    /// Gets the other instances of the same <see cref="AspectClass"/> on the same <see cref="IAspectPredecessor.TargetDeclaration"/>.
    /// When several instances of the same <see cref="AspectClass"/> are found on the same <see cref="IAspectPredecessor.TargetDeclaration"/>,
    /// they are ordered by priority, and only the first one gets executed. The other instances are exposed on this property.
    /// </summary>
    ImmutableArray<IIntrospectionAspectInstance> SecondaryInstances { get; }

    /// <summary>
    /// Gets the optional opaque object defined by the aspect for the specific <see cref="IAspectPredecessor.TargetDeclaration"/> using the <see cref="IAspectBuilder.AspectState"/>
    /// property of the <see cref="IAspectBuilder"/> interface.
    /// </summary>
    IAspectState? AspectState { get; }
}