// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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