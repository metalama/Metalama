// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed partial class RefFactory
{
    private sealed class SymbolCacheKeyComparer : IEqualityComparer<SymbolCacheKey>
    {
        public bool Equals( SymbolCacheKey x, SymbolCacheKey y )
            => x.Symbol.Equals( y.Symbol, SymbolEqualityComparer.IncludeNullability ) && x.TargetKind == y.TargetKind
                                                                                      && x.GenericContext.Equals( y.GenericContext );

        public int GetHashCode( SymbolCacheKey obj )
            => HashCode.Combine( SymbolEqualityComparer.IncludeNullability.GetHashCode( obj.Symbol ), (int) obj.TargetKind, obj.GenericContext );
    }

    private sealed class TupleTypeSymbolCacheKeyComparer : IEqualityComparer<TupleTypeSymbolCacheKey>
    {
        public bool Equals( TupleTypeSymbolCacheKey x, TupleTypeSymbolCacheKey y )
        {
            if ( !x.Symbol.Equals( y.Symbol, SymbolEqualityComparer.IncludeNullability ) )
            {
                return false;
            }

            if ( !x.GenericContext.Equals( y.GenericContext ) )
            {
                return false;
            }

            for ( var i = 0; i < x.ElementNames.Length; i++ )
            {
                if ( x.ElementNames[i] != y.ElementNames[i] )
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode( TupleTypeSymbolCacheKey obj )
        {
            var hashCode = default(HashCode);
            hashCode.Add( SymbolEqualityComparer.IncludeNullability.GetHashCode( obj.Symbol ) );
            hashCode.Add( obj.GenericContext );

            foreach ( var e in obj.ElementNames )
            {
                hashCode.Add( e );
            }

            return hashCode.ToHashCode();
        }
    }
}