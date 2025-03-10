// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// A class that, when inherited by a type in a given namespace, allows that type to analyze and
    /// add aspects to that namespace.
    /// </summary>
    /// <seealso href="@fabrics"/> 
    /// <seealso href="@aspect-configuration"/>
    /// <seealso href="@fabrics-adding-aspects"/>
    public abstract class NamespaceFabric : Fabric
    {
        /// <summary>
        /// The user can implement this method to analyze types in the current namespace, add aspects, and report or suppress diagnostics.
        /// </summary>
        public abstract void AmendNamespace( INamespaceAmender amender );
    }
}