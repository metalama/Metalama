// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel.Factories;

public partial class DeclarationFactory
{
    private struct TupleTypeKey : IEquatable<TupleTypeKey>
    {
        public INamedTypeSymbol Symbol { get; }

        public ImmutableArray<string> ElementNames { get; }

        public TupleTypeKey( INamedTypeSymbol symbol, ImmutableArray<string> elementNames )
        {
            this.Symbol = symbol;
            this.ElementNames = elementNames;
        }

        public readonly bool Equals( TupleTypeKey other )
        {
            if ( !SymbolEqualityComparer.IncludeNullability.Equals( this.Symbol, other.Symbol ) )
            {
                return false;
            }

            for ( var i = 0; i < this.ElementNames.Length; i++ )
            {
                if ( this.ElementNames[i] != other.ElementNames[i] )
                {
                    return false;
                }
            }

            return true;
        }

        public override readonly int GetHashCode()
        {
            var hashCode = default(HashCode);
            hashCode.Add( SymbolEqualityComparer.IncludeNullability.GetHashCode( this.Symbol ) );

            foreach ( var e in this.ElementNames )
            {
                hashCode.Add( e );
            }

            return hashCode.ToHashCode();
        }
    }
}