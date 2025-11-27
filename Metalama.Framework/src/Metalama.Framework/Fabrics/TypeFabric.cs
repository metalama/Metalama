// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// An class that, when inherited by a nested type in a given type, allows that nested type to analyze and
    /// add aspects to the parent type.
    /// </summary>
    /// <seealso cref="Fabric"/>
    /// <seealso cref="ITypeAmender"/>
    /// <seealso cref="ProjectFabric"/>
    /// <seealso cref="NamespaceFabric"/>
    /// <seealso href="@fabrics"/>
    /// <seealso href="@aspect-configuration"/>
    /// <seealso href="@fabrics-adding-aspects"/>
    [PublicAPI]
    public abstract class TypeFabric : Fabric
    {
        /// <summary>
        /// The user can implement this method to analyze types in the declaring type, add aspects, set options, validate architecture, and report or suppress diagnostics.
        /// </summary>
        /// <param name="amender">An object that allows to query declarations in the type, add aspects, and set options.</param>
        public virtual void AmendType( ITypeAmender amender ) { }
    }
}