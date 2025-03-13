// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.ReferenceGraph;

public readonly struct ReferencingSymbolInfo
{
    public ReferencingSymbolInfo( ISymbol referencingSymbol, ISymbol referencedSymbol, ReferencingNodeList nodes )
    {
        this.ReferencingSymbol = referencingSymbol;
        this.ReferencedSymbol = referencedSymbol;
        this.Nodes = nodes;
    }

    public ISymbol ReferencingSymbol { get; }

    public ISymbol ReferencedSymbol { get; }

    public ReferencingNodeList Nodes { get; }
}