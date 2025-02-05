// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Collections.Immutable;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Base interface for objects that can cause aspects to be added to a compilation. Predecessors are exposed on
/// the <see cref="IAspectPredecessor.Predecessors"/> property.
/// </summary>
[PublicAPI]
public interface IIntrospectionAspectPredecessor
{
    /// <summary>
    /// Gets the number of predecessors between the root cause and the current predecessor, or <c>0</c>
    /// if the current predecessor is the root cause. 
    /// </summary>
    int PredecessorDegree { get; }

    /// <summary>
    /// Gets the declaration to which the aspect or fabric is applied.
    /// </summary>
    IDeclaration TargetDeclaration { get; }

    /// <summary>
    /// Gets the list of objects that have caused the current aspect instance (but not any instance in the <see cref="IIntrospectionAspectInstance.SecondaryInstances"/> list)
    /// to be created.
    /// </summary>
    /// <seealso href="@child-aspects"/>
    ImmutableArray<IntrospectionAspectRelationship> Predecessors { get; }

    /// <summary>
    /// Gets the list of aspect instances that have been created (or caused) by the current object.
    /// </summary>
    ImmutableArray<IntrospectionAspectRelationship> Successors { get; }
}