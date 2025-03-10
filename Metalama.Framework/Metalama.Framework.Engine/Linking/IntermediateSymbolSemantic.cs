// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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