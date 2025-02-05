// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed partial class RefFactory
{
    private sealed class SymbolCacheKeyComparer : IEqualityComparer<SymbolCacheKey>
    {
        public bool Equals( SymbolCacheKey x, SymbolCacheKey y )
            => x.Symbol.Equals( y.Symbol, SymbolEqualityComparer.IncludeNullability ) && x.TargetKind == y.TargetKind && x.GenericContext.Equals( y.GenericContext );

        public int GetHashCode( SymbolCacheKey obj )
            => HashCode.Combine( SymbolEqualityComparer.IncludeNullability.GetHashCode( obj.Symbol ), (int) obj.TargetKind, obj.GenericContext );
    }
}