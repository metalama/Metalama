// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// Represents a <see cref="Fabric"/> as an <see cref="IAspectPredecessor"/>, allowing fabrics to participate
    /// in the aspect predecessor chain when they cause aspects to be added to declarations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface serves as a wrapper that presents a <see cref="Fabric"/> within the <see cref="IAspectPredecessor"/> model.
    /// When a fabric adds an aspect to a declaration (via <see cref="ProjectFabric.AmendProject"/>, <see cref="NamespaceFabric.AmendNamespace"/>,
    /// or <see cref="TypeFabric.AmendType"/>), the fabric instance becomes a predecessor of that aspect instance, appearing in the aspect's
    /// <see cref="IAspectPredecessor.Predecessors"/> collection.
    /// </para>
    /// <para>
    /// The <see cref="IAspectPredecessor.TargetDeclaration"/> property identifies the declaration scope where the fabric executes:
    /// the compilation for <see cref="ProjectFabric"/>, the namespace for <see cref="NamespaceFabric"/>, or the containing type for <see cref="TypeFabric"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IAspectPredecessor"/>
    /// <seealso cref="Fabric"/>
    /// <seealso href="@fabrics"/>
    /// <seealso href="@child-aspects"/>
    [CompileTime]
    public interface IFabricInstance : IAspectPredecessor
    {
        /// <summary>
        /// Gets the <see cref="Fabric"/> instance.
        /// </summary>
        Fabric Fabric { get; }
    }
}