// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Utilities;
using System.Collections.Immutable;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Represents an instance of an aspect. The instance of the <see cref="IAspect"/> itself is in the <see cref="Aspect"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Use Cases:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <b>Accessing the current aspect instance:</b> In the <see cref="IAspect{T}.BuildAspect"/> method, access the current
    /// aspect instance via <see cref="IAspectBuilder.AspectInstance"/> to retrieve aspect-specific information such as
    /// <see cref="AspectClass"/>, <see cref="AspectState"/>, <see cref="SecondaryInstances"/>, or <see cref="IAspectPredecessor.Predecessors"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>In templates:</b> Access the current aspect instance via <c>meta.AspectInstance</c> to retrieve aspect-specific information
    /// during code generation.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Querying aspects on declarations:</b> Retrieve aspect instances applied to any declaration using
    /// <see cref="DeclarationEnhancements{T}.GetAspectInstances"/> to inspect which aspects have been applied and their state.
    /// This can only query aspects that have already been applied or are being applied (e.g., aspects ordered before the current one,
    /// or instances of the current aspect applied in a parent class).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>In aspect predecessor chains:</b> When examining <see cref="IAspectPredecessor.Predecessors"/>, predecessor instances
    /// can be cast to <see cref="IAspectInstance"/> when <see cref="AspectPredecessor.Kind"/> indicates an aspect relationship.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IAspect"/>
    /// <seealso cref="IAspectClass"/>
    /// <seealso cref="IAspectBuilder"/>
    /// <seealso cref="IAspectState"/>
    /// <seealso cref="IAspectPredecessor"/>
    /// <seealso href="@child-aspects"/>
    [InternalImplement]
    [CompileTime]
    public interface IAspectInstance : IAspectPredecessor
    {
        /// <summary>
        /// Gets the aspect instance.
        /// </summary>
        IAspect Aspect { get; }

        /// <summary>
        /// Gets the aspect type.
        /// </summary>
        IAspectClass AspectClass { get; }

        /// <summary>
        /// Gets a value indicating whether the current aspect instance has been skipped. This value is <c>true</c> if
        /// the aspect evaluation resulted in an error or if the <see cref="IAspect{T}.BuildAspect"/> method invoked
        /// <see cref="IAspectBuilder.SkipAspect"/>, if it has been excluded using <see cref="ExcludeAspectAttribute"/>,
        /// or when the target declaration was not eligible.
        /// </summary>
        bool IsSkipped { get; }

        /// <summary>
        /// Gets a value indicating whether the current aspect instance can be inherited by derived declarations.
        /// </summary>
        bool IsInheritable { get; }

        /// <summary>
        /// Gets the other instances of the same <see cref="AspectClass"/> on the same <see cref="IAspectPredecessor.TargetDeclaration"/>.
        /// When several instances of the same <see cref="AspectClass"/> are found on the same <see cref="IAspectPredecessor.TargetDeclaration"/>,
        /// they are ordered by priority, and only the first one gets executed. The other instances are exposed on this property.
        /// </summary>
        ImmutableArray<IAspectInstance> SecondaryInstances { get; }

        /// <summary>
        /// Gets the optional opaque object defined by the aspect for the specific <see cref="IAspectPredecessor.TargetDeclaration"/> using the <see cref="IAspectBuilder.AspectState"/>
        /// property of the <see cref="IAspectBuilder"/> interface.
        /// </summary>
        IAspectState? AspectState { get; }
    }
}