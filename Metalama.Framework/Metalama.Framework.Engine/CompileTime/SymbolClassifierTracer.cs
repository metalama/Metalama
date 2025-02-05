// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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