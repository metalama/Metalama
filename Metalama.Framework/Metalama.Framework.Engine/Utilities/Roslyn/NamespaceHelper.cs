// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

internal static class NamespaceHelper
{
    private static readonly WeakCache<INamespaceOrTypeSymbol, string?> _fullNameCache = new();

    public static string? GetFullName( this INamespaceOrTypeSymbol? symbol ) => symbol == null ? null : _fullNameCache.GetOrAdd( symbol, GetFullNameImpl );

    public static INamespaceOrTypeSymbol? GetFirstLevel( this INamespaceSymbol ns )
    {
        for ( var n = ns; !n.IsGlobalNamespace; n = n.ContainingNamespace )
        {
            if ( n.ContainingNamespace.IsGlobalNamespace )
            {
                return n;
            }
        }

        return null;
    }

    private static string? GetFullNameImpl( this INamespaceOrTypeSymbol? symbol )
    {
        if ( symbol is null or INamespaceSymbol { IsGlobalNamespace: true } )
        {
            return null;
        }

        using var stringBuilder = StringBuilderPool.Default.Allocate();

        void AppendNameRecursive( ISymbol s )
        {
            var (parent, separator) = s switch
            {
                INamedTypeSymbol { ContainingType: { } containingType } => (containingType, '.'),
                INamedTypeSymbol namedType => (namedType.ContainingNamespace, '.'),
                INamespaceSymbol ns => (ns.ContainingNamespace, '.'),
                _ => (s.ContainingSymbol, '.')
            };

            if ( parent != null )
            {
                AppendNameRecursive( parent );
            }

            if ( stringBuilder.Value.Length > 0 )
            {
                stringBuilder.Value.Append( separator );
            }

            stringBuilder.Value.Append( s.Name );
        }

        AppendNameRecursive( symbol );

        return stringBuilder.Value.ToString();
    }
}