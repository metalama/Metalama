// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed partial class RefFactory
{
    private readonly struct SymbolCacheKey : IEquatable<SymbolCacheKey>
    {
        private SymbolCacheKey( ISymbol symbol, RefTargetKind targetKind, GenericContext genericContext )
        {
            this.Symbol = symbol;
            this.TargetKind = targetKind;
            this.GenericContext = genericContext;
        }

        public static SymbolCacheKey Create( ISymbol symbol, RefTargetKind targetKind, GenericContext genericContext, RefFactory refFactory )
        {
            var canonical = SymbolNormalizer.GetCanonicalSymbol( symbol, genericContext, refFactory );

            return new SymbolCacheKey( canonical.Symbol, targetKind, canonical.Context );
        }

        public ISymbol Symbol { get; }

        public RefTargetKind TargetKind { get; }

        public GenericContext GenericContext { get; }

        public bool Equals( SymbolCacheKey other )
            => this.Symbol.Equals( other.Symbol, SymbolEqualityComparer.IncludeNullability ) && this.TargetKind == other.TargetKind
                                                                                             && this.GenericContext.Equals( other.GenericContext );

        public override bool Equals( object? obj ) => obj is SymbolCacheKey other && this.Equals( other );

        public override int GetHashCode()
            => HashCode.Combine( SymbolEqualityComparer.IncludeNullability.GetHashCode( this.Symbol ), (int) this.TargetKind, this.GenericContext );
    }
}