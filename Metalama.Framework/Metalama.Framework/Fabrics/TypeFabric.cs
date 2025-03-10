// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// An class that, when inherited by a nested type in a given type, allows that nested type to analyze and
    /// add aspects to the parent type.
    /// </summary>
    /// <seealso href="@fabrics"/> 
    [PublicAPI]
    public abstract class TypeFabric : Fabric
    {
        /// <summary>
        /// The user can implement this method to analyze types in the declaring type, add aspects, and report or suppress diagnostics.
        /// </summary>
        public virtual void AmendType( ITypeAmender amender ) { }
    }
}