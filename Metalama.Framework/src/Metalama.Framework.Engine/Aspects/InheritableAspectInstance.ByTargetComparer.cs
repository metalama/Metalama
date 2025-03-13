// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Aspects;

public sealed partial class InheritableAspectInstance
{
    /// <summary>
    /// Compares two instances of the <see cref="InheritableAspectInstance"/> by target declaration. It is correct to use this comparer
    /// in a context where all instances are of the same class because we cannot have several instances of the same aspect class on the
    /// same target class.
    /// </summary>
    public sealed class ByTargetComparer : IEqualityComparer<InheritableAspectInstance>
    {
        public static ByTargetComparer Instance { get; } = new();

        public bool Equals( InheritableAspectInstance? x, InheritableAspectInstance? y )
        {
            if ( ReferenceEquals( x, y ) )
            {
                return true;
            }

            if ( x == null || y == null )
            {
                return false;
            }

            if ( x.GetType() != y.GetType() )
            {
                return false;
            }

            return x.TargetDeclaration.Equals( y.TargetDeclaration, RefComparison.Structural );
        }

        public int GetHashCode( InheritableAspectInstance obj )
        {
            return obj.TargetDeclaration.GetHashCode( RefComparison.Structural );
        }
    }
}