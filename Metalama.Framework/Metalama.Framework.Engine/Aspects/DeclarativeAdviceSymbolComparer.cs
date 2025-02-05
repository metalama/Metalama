// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Comparers;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// Compares two symbols representing declarative advice so that their processing order can be determined.
/// </summary>
internal sealed class DeclarativeAdviceSymbolComparer : IComparer<ISymbol>
{
    public static DeclarativeAdviceSymbolComparer Instance { get; } = new();

    private DeclarativeAdviceSymbolComparer() { }

    private static readonly ImmutableDictionary<(SymbolKind, MethodKind), int> _orderedSymbolKinds =
        new[]
            {
                (SymbolKind.Field, default),
                (SymbolKind.Method, MethodKind.StaticConstructor),
                (SymbolKind.Method, MethodKind.Constructor),
                (SymbolKind.Method, MethodKind.Destructor),
                (SymbolKind.Property, default),
                (SymbolKind.Event, default(MethodKind)),
                (SymbolKind.Method, MethodKind.Ordinary),
                (SymbolKind.Method, MethodKind.UserDefinedOperator),
                (SymbolKind.Method, MethodKind.Conversion)
            }.Select( ( x, i ) => (Kind: x, Index: i) )
            .ToImmutableDictionary( x => x.Kind, x => x.Index );

    private static int GetOrderByKind( ISymbol symbol )
    {
        var kind = symbol is IMethodSymbol methodSymbol ? (SymbolKind.Method, methodSymbol.MethodKind) : (symbol.Kind, default);

        if ( _orderedSymbolKinds.TryGetValue( kind, out var order ) )
        {
            return order;
        }
        else
        {
            return int.MaxValue;
        }
    }

    public int Compare( ISymbol? x, ISymbol? y )
    {
        if ( ReferenceEquals( x, y ) )
        {
            return 0;
        }
        else if ( x == null )
        {
            return 1;
        }
        else if ( y == null )
        {
            return -1;
        }

        var compareByKind = GetOrderByKind( x ).CompareTo( GetOrderByKind( y ) );

        if ( compareByKind != 0 )
        {
            return compareByKind;
        }

        var compareByName = StringComparer.Ordinal.Compare( x.Name, y.Name );

        if ( compareByName != 0 )
        {
            return compareByName;
        }

        return StructuralSymbolComparer.Default.Compare( x, y );
    }
}