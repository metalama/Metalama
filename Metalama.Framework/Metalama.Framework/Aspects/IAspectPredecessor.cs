// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Utilities;
using System.Collections.Immutable;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Base interface for objects that can cause aspects to be added to a compilation. Predecessors are exposed on
    /// the <see cref="IAspectPredecessor.Predecessors"/> property.
    /// </summary>
    [CompileTime]
    [InternalImplement]
    public interface IAspectPredecessor
    {
        /// <summary>
        /// Gets the number of predecessors between the root cause and the current predecessor, or <c>0</c>
        /// if the current predecessor is the root cause. 
        /// </summary>
        int PredecessorDegree { get; }

        /// <summary>
        /// Gets the declaration to which the aspect or fabric is applied.
        /// </summary>
        IRef<IDeclaration> TargetDeclaration { get; }

        /// <summary>
        /// Gets the list of objects that have caused the current aspect instance (but not any instance in the <see cref="IAspectInstance.SecondaryInstances"/> list)
        /// to be created. The ordering of this list is undetermined.
        /// </summary>
        /// <seealso href="@child-aspects"/>
        ImmutableArray<AspectPredecessor> Predecessors { get; }
    }
}