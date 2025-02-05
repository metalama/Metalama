// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Code.Collections
{
    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Instance = new();

        public bool Equals( T? x, T? y ) => ReferenceEquals( x, y );

        public int GetHashCode( T obj ) => RuntimeHelpers.GetHashCode( obj );
    }
}