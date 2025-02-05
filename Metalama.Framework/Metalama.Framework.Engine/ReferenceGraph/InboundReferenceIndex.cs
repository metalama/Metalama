// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.ReferenceGraph;

public sealed class InboundReferenceIndex
{
    private readonly ConcurrentDictionary<ISymbol, ReferencedSymbolInfo> _explicitReferences;

    internal InboundReferenceIndex( ConcurrentDictionary<ISymbol, ReferencedSymbolInfo> explicitReferences )
    {
        this._explicitReferences = explicitReferences;
    }

    public IEnumerable<ReferencedSymbolInfo> ReferencedSymbols => this._explicitReferences.Values;

    internal bool TryGetInboundReferences( ISymbol referencedSymbol, [NotNullWhen( true )] out ReferencedSymbolInfo? referencedSymbolInfo )
        => this._explicitReferences.TryGetValue( referencedSymbol, out referencedSymbolInfo );
}