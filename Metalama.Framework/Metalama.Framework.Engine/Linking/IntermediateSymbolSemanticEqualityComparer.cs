// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

#pragma warning disable SA1402 // File may only contain a single type

namespace Metalama.Framework.Engine.Linking;

internal sealed class IntermediateSymbolSemanticEqualityComparer : IEqualityComparer<IntermediateSymbolSemantic>
{
    private readonly IEqualityComparer<ISymbol> _symbolComparer;

    private IntermediateSymbolSemanticEqualityComparer( IEqualityComparer<ISymbol> symbolComparer )
    {
        this._symbolComparer = symbolComparer;
    }

    public static IEqualityComparer<IntermediateSymbolSemantic> ForCompilation( CompilationContext context )
        => new IntermediateSymbolSemanticEqualityComparer( context.SymbolComparer );

    public bool Equals( IntermediateSymbolSemantic x, IntermediateSymbolSemantic y )
        => this._symbolComparer.Equals( x.Symbol, y.Symbol )
           && x.Kind == y.Kind;

    public int GetHashCode( IntermediateSymbolSemantic x )
        =>

            // PERF: Cast enum to byte otherwise it will be boxed within the method.
            HashCode.Combine(
                this._symbolComparer.GetHashCode( x.Symbol ),
                (byte) x.Kind );
}

internal sealed class IntermediateSymbolSemanticEqualityComparer<TSymbol> : IEqualityComparer<IntermediateSymbolSemantic<TSymbol>>
    where TSymbol : ISymbol
{
    private readonly IEqualityComparer<ISymbol> _symbolComparer;

    private IntermediateSymbolSemanticEqualityComparer( IEqualityComparer<ISymbol> symbolComparer )
    {
        this._symbolComparer = symbolComparer;
    }

    public static IEqualityComparer<IntermediateSymbolSemantic<TSymbol>> ForCompilation( CompilationContext context )
        => new IntermediateSymbolSemanticEqualityComparer<TSymbol>( context.SymbolComparer );

    public bool Equals( IntermediateSymbolSemantic<TSymbol> x, IntermediateSymbolSemantic<TSymbol> y )
        => this._symbolComparer.Equals( x.Symbol, y.Symbol )
           && x.Kind == y.Kind;

    public int GetHashCode( IntermediateSymbolSemantic<TSymbol> x )
        =>

            // PERF: Cast enum to byte otherwise it will be boxed within the method.
            HashCode.Combine(
                this._symbolComparer.GetHashCode( x.Symbol ),
                (byte) x.Kind );
}