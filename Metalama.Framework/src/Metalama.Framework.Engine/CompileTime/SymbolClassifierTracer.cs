// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed class SymbolClassifierTracer
{
    private SymbolClassifierTracer( ISymbol symbol, int depth )
    {
        this.Symbol = symbol;
        this.Depth = depth;
    }

    public SymbolClassifierTracer( ISymbol symbol ) : this( symbol, 0 ) { }

    public TemplatingScope? Result { get; private set; }

    public ISymbol? Symbol { get; }

    public int Depth { get; }

    public void SetResult( TemplatingScope? result ) => this.Result = result;

    public List<SymbolClassifierTracer> Children { get; } = new();

    public SymbolClassifierTracer CreateChild( ISymbol symbol )
    {
        var child = new SymbolClassifierTracer( symbol, this.Depth + 1 );
        this.Children.Add( child );

        return child;
    }
}