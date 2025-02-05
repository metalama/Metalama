// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;

#pragma warning disable SA1414

namespace Metalama.Framework.Engine.Utilities;

internal static class ValueTupleComparer
{
    public static IEqualityComparer<(T1, T2)> Create<T1, T2>( IEqualityComparer<T1> c1, IEqualityComparer<T2> c2 ) => new Comparer<T1, T2>( c1, c2 );

    private sealed class Comparer<T1, T2> : IEqualityComparer<(T1, T2)>
    {
        private readonly IEqualityComparer<T1> _c1;
        private readonly IEqualityComparer<T2> _c2;

        public Comparer( IEqualityComparer<T1> c1, IEqualityComparer<T2> c2 )
        {
            this._c1 = c1;
            this._c2 = c2;
        }

        public bool Equals( (T1, T2) x, (T1, T2) y )
        {
            return this._c1.Equals( x.Item1, y.Item1 ) && this._c2.Equals( x.Item2, y.Item2 );
        }

        public int GetHashCode( (T1, T2) x )
        {
            return HashCode.Combine(
                x.Item1 is not null ? this._c1.GetHashCode( x.Item1! ) : 0,
                x.Item2 is not null ? this._c2.GetHashCode( x.Item2! ) : 0 );
        }
    }
}