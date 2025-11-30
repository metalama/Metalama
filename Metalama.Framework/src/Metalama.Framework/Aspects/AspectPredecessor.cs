// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Represents one link in the causality chain that led to an aspect being applied. Each predecessor describes
    /// what caused an aspect instance to be created (e.g., a custom attribute, a fabric, a parent aspect, or inheritance).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Aspect predecessors form a chain that tracks the provenance of aspect instances. This is primarily useful for:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Parent-child communication:</b> Child aspects can access parent aspect state via <see cref="IAspectPredecessor.Predecessors"/></description></item>
    /// <item><description><b>Introspection:</b> Analyzing aspect composition and dependencies programmatically using the <c>Metalama.Framework.Introspection</c> namespace</description></item>
    /// </list>
    /// <para>
    /// Use the <see cref="Kind"/> property to determine the type of predecessor, then cast <see cref="Instance"/> to the
    /// appropriate type: <see cref="IAspectInstance"/>, <see cref="IFabricInstance"/>, or <see cref="IAttribute"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IAspectPredecessor"/>
    /// <seealso cref="AspectPredecessorKind"/>
    /// <seealso cref="IAspectInstance"/>
    /// <seealso cref="IAspectState"/>
    /// <seealso href="@child-aspects"/>
    /// <seealso href="@introspection"/>
    [CompileTime]
    public readonly struct AspectPredecessor
    {
        /// <summary>
        /// Gets the kind of relationship represented by the current <see cref="AspectPredecessor"/>, and the kind of object
        /// present in the <see cref="Instance"/> property. 
        /// </summary>
        public AspectPredecessorKind Kind { get; }

        /// <summary>
        /// Gets the object that created the aspect instance. It can be an <see cref="IAspectInstance"/>, an <see cref="IFabricInstance"/>, or an <see cref="IAttribute"/>.
        /// </summary>
        public IAspectPredecessor Instance { get; }

        internal AspectPredecessor( AspectPredecessorKind kind, IAspectPredecessor instance )
        {
            this.Kind = kind;
            this.Instance = instance;
        }

        public override string ToString() => $"Kind={this.Kind}, Instance={{{this.Instance}}}";
    }
}