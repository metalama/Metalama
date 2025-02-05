// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Utilities.Comparers;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Linking;

internal readonly struct IntermediateSymbolSemantic : IEquatable<IntermediateSymbolSemantic>
{
    public ISymbol Symbol { get; }

    public IntermediateSymbolSemanticKind Kind { get; }

    public IntermediateSymbolSemantic( ISymbol symbol, IntermediateSymbolSemanticKind semantic )
    {
        this.Symbol = symbol.GetCanonicalDefinition();
        this.Kind = semantic;
    }

    public bool Equals( IntermediateSymbolSemantic other )
        => StructuralSymbolComparer.Default.Equals( this.Symbol.OriginalDefinition, other.Symbol.OriginalDefinition )
           && other.Kind == this.Kind;

    // PERF: Cast enum to int otherwise it will be boxed on .NET Framework.
    public override int GetHashCode()
        => HashCode.Combine(
            StructuralSymbolComparer.Default.GetHashCode( this.Symbol ),
            (int) this.Kind );

    public IntermediateSymbolSemantic<TSymbol> ToTyped<TSymbol>()
        where TSymbol : ISymbol
        => new( (TSymbol) this.Symbol, this.Kind );

    public IntermediateSymbolSemantic WithSymbol( ISymbol symbol ) => new( symbol, this.Kind );

    public IntermediateSymbolSemantic<TSymbol> WithSymbol<TSymbol>( TSymbol symbol )
        where TSymbol : ISymbol
        => new( symbol, this.Kind );

    // Coverage: ignore (useful for debugging)
    public override string ToString() => $"{{{this.Kind}, {this.Symbol.ToDebugString()}}}";
}

internal readonly struct IntermediateSymbolSemantic<TSymbol> : IEquatable<IntermediateSymbolSemantic<TSymbol>>
    where TSymbol : ISymbol
{
    public TSymbol Symbol { get; }

    public IntermediateSymbolSemanticKind Kind { get; }

    public IntermediateSymbolSemantic( TSymbol symbol, IntermediateSymbolSemanticKind semantic )
    {
        this.Symbol = symbol;
        this.Kind = semantic;
    }

    public bool Equals( IntermediateSymbolSemantic<TSymbol> other )
        => StructuralSymbolComparer.Default.Equals( this.Symbol.OriginalDefinition, other.Symbol.OriginalDefinition )
           && other.Kind == this.Kind;

    // PERF: Cast enum to byte otherwise it will be boxed on .NET Framework.
    public override int GetHashCode()
        => HashCode.Combine(
            StructuralSymbolComparer.Default.GetHashCode( this.Symbol ),
            (byte) this.Kind );

    public static implicit operator IntermediateSymbolSemantic( IntermediateSymbolSemantic<TSymbol> value ) => new( value.Symbol, value.Kind );

    // Coverage: ignore (useful for debugging)
    public override string ToString() => $"{{{this.Kind}, {this.Symbol.ToDebugString()}}}";
}