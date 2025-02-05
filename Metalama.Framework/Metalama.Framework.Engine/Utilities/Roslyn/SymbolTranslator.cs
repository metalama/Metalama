// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

internal sealed partial class SymbolTranslator
{
    private readonly ConcurrentDictionary<(ISymbol, bool), ISymbol?> _cache;
    private readonly CompilationContext _targetCompilationContext;

    internal SymbolTranslator( CompilationContext targetCompilationContextContext )
    {
        this._cache = new ConcurrentDictionary<(ISymbol, bool), ISymbol?>( KeyComparer.Instance );

        this._targetCompilationContext = targetCompilationContextContext;
    }

    public T? Translate<T>( T symbol, bool allowMultiple = false, CompilationContext? symbolCompilationContext = null )
        where T : ISymbol
    {
        if ( symbolCompilationContext == this._targetCompilationContext )
        {
            return symbol;
        }
        
        var containingAssembly = symbol.ContainingAssembly;

        if ( containingAssembly != null && this._targetCompilationContext.Assemblies.TryGetValue( containingAssembly.Identity, out var assembly )
                                        && assembly.Equals( containingAssembly ) )
        {
            // The symbol is guaranteed to be in the same assembly.
            return symbol;
        }
        else
        {
            return (T?) this._cache.GetOrAdd( (symbol, allowMultiple), this.TranslateCore );
        }
    }

    public T? Translate<T>( T symbol, Compilation? originalCompilation, bool allowMultiple = false )
        where T : ISymbol
    {
        if ( originalCompilation == this._targetCompilationContext.Compilation )
        {
            return symbol;
        }
        else
        {
            return (T?) this._cache.GetOrAdd( (symbol, allowMultiple), this.TranslateCore );
        }
    }

    private ISymbol? TranslateCore( (ISymbol Symbol, bool AllowMultipleCandidates) value )
        => new Visitor( this, value.AllowMultipleCandidates ).Visit( value.Symbol );

    private sealed class KeyComparer : IEqualityComparer<(ISymbol Symbol, bool AllowMultipleCandidates)>
    {
        public static readonly KeyComparer Instance = new();

        public bool Equals( (ISymbol Symbol, bool AllowMultipleCandidates) x, (ISymbol Symbol, bool AllowMultipleCandidates) y )
        {
            return
                ReferenceEqualityComparer<ISymbol>.Instance.Equals( x.Symbol, y.Symbol )
                && x.AllowMultipleCandidates == y.AllowMultipleCandidates;
        }

        public int GetHashCode( (ISymbol Symbol, bool AllowMultipleCandidates) value )
        {
            return HashCode.Combine( ReferenceEqualityComparer<ISymbol>.Instance.GetHashCode( value.Symbol ), value.AllowMultipleCandidates.GetHashCode() );
        }
    }
}