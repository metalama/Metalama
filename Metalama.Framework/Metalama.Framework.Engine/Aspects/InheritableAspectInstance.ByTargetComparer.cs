// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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