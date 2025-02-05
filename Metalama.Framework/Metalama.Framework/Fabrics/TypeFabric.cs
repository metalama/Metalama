// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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