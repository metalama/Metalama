// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Generic;

namespace Metalama.Framework.Code.Comparers;

public sealed class RefEqualityComparer : IEqualityComparer<IRef>, IRefEqualityComparer
{
    private readonly RefComparison _comparison;

    public static RefEqualityComparer Default { get; } = new( RefComparison.Default );

    public static RefEqualityComparer IncludeNullability { get; } = new( RefComparison.IncludeNullability );

    public static RefEqualityComparer Structural { get; } = new( RefComparison.Structural );

    public static RefEqualityComparer StructuralIncludeNullability { get; } =
        new( RefComparison.StructuralIncludeNullability );

    private RefEqualityComparer( RefComparison comparison )
    {
        this._comparison = comparison;
    }

    public bool Equals( IRef? x, IRef? y )
    {
        if ( ReferenceEquals( x, y ) )
        {
            return true;
        }

        if ( x == null || y == null )
        {
            return false;
        }

        return x.Equals( y, this._comparison );
    }

    public int GetHashCode( IRef obj ) => obj.GetHashCode( this._comparison );
}