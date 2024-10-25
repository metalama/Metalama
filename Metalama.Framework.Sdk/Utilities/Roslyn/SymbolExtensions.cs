// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

public static class SymbolExtensions
{
    public static SyntaxReference? GetPrimarySyntaxReference( this ISymbol? symbol )
    {
        if ( symbol == null )
        {
            return null;
        }

        static SyntaxReference? GetReferenceOfShortestPath( ISymbol s, Func<SyntaxReference, bool>? filter = null )
        {
            if ( s.DeclaringSyntaxReferences.IsDefaultOrEmpty )
            {
                return null;
            }
            else
            {
                // Find the lowest value.

                SyntaxReference? min = null;
                int? minLength = null;

                foreach ( var reference in s.DeclaringSyntaxReferences )
                {
                    if ( filter != null && !filter( reference ) )
                    {
                        continue;
                    }

                    var length = reference.SyntaxTree.FilePath.Length;

                    if ( min == null || length < minLength )
                    {
                        min = reference;
                        minLength = length;
                    }
                }

                return min;
            }
        }

        switch ( symbol )
        {
            case IMethodSymbol { AssociatedSymbol: { } associatedSymbol }:
                return GetReferenceOfShortestPath( symbol ) ?? GetReferenceOfShortestPath( associatedSymbol );

            case IMethodSymbol { IsPartialDefinition: true, PartialImplementationPart: { } partialImplementationSymbol }:
                return GetReferenceOfShortestPath( partialImplementationSymbol );

            default:
                return GetReferenceOfShortestPath( symbol );
        }
    }

    public static SyntaxNode? GetPrimaryDeclarationSyntax( this ISymbol symbol ) => symbol.GetPrimarySyntaxReference()?.GetSyntax();
}