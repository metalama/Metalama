// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Utilities;
using System.Collections.Immutable;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Base interface for objects that can cause aspects to be added to a compilation, such as aspect instances and fabric instances.
    /// This interface tracks the chain of causality that led to an aspect being applied, accessible through the <see cref="Predecessors"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface is implemented by <see cref="IAspectInstance"/>, <see cref="IFabricInstance"/>, and <see cref="IAttribute"/>,
    /// establishing a unified model for tracking what caused aspects to be added to declarations.
    /// </para>
    /// <para>
    /// The primary use case for predecessors when building aspects is to access the state of parent aspects through <see cref="IAspectState"/>.
    /// When a parent aspect creates child aspects, the child aspects can query their <see cref="Predecessors"/> to find the parent aspect instance
    /// and access its state, enabling communication and data sharing between parent and child aspects in the aspect composition hierarchy.
    /// </para>
    /// <para>
    /// Predecessors also serve secondary purposes for debugging, introspection, and understanding aspect composition and provenance.
    /// </para>
    /// <para>
    /// For example:
    /// </para>
    /// <list type="bullet">
    /// <item><description>When a fabric adds an aspect to a type, that fabric instance becomes a predecessor of the aspect instance.</description></item>
    /// <item><description>When an aspect creates child aspects, the parent aspect instance becomes a predecessor of the child aspect instances, allowing children to access parent state.</description></item>
    /// <item><description>When a custom attribute that is also an aspect (e.g., a class implementing <see cref="IAspect"/>) is applied to a declaration, the attribute becomes a predecessor.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IAspectInstance"/>
    /// <seealso cref="IAspectState"/>
    /// <seealso cref="IFabricInstance"/>
    /// <seealso cref="IAttribute"/>
    /// <seealso href="@child-aspects"/>
    /// <seealso href="@introspection"/>
    [CompileTime]
    [InternalImplement]
    public interface IAspectPredecessor
    {
        /// <summary>
        /// Gets the depth in the predecessor chain, indicating how many levels of indirection exist between
        /// the root cause and this predecessor. A value of <c>0</c> means this is the root cause (e.g., a custom
        /// attribute or fabric that directly applied the aspect).
        /// </summary>
        /// <remarks>
        /// <para>
        /// For example: If a fabric (degree 0) adds aspect A, which then adds child aspect B (degree 1),
        /// which adds child aspect C (degree 2), then C's <see cref="PredecessorDegree"/> is 2.
        /// </para>
        /// <para>
        /// This property is primarily used for debugging and introspection to understand the chain of causality.
        /// </para>
        /// </remarks>
        int PredecessorDegree { get; }

        /// <summary>
        /// Gets the declaration to which the aspect or fabric is applied.
        /// </summary>
        IRef<IDeclaration> TargetDeclaration { get; }

        /// <summary>
        /// Gets the list of objects that have caused the current aspect instance (but not any instance in the <see cref="IAspectInstance.SecondaryInstances"/> list)
        /// to be created. The ordering of this list is undetermined and should not be relied upon.
        /// </summary>
        /// <remarks>
        /// <para>
        /// An aspect can have multiple predecessors when it's applied through multiple mechanisms simultaneously
        /// (e.g., both inherited from a base class and added by a fabric). The order of predecessors in this array
        /// is implementation-defined and may vary between builds.
        /// </para>
        /// <para>
        /// To find a specific predecessor type, iterate through the array and check each <see cref="AspectPredecessor.Kind"/>.
        /// </para>
        /// </remarks>
        /// <seealso href="@child-aspects"/>
        ImmutableArray<AspectPredecessor> Predecessors { get; }
    }
}