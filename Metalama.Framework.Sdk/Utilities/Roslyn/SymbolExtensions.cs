// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

public static class SymbolExtensions
{
    private static readonly Func<IPropertySymbol, bool> _isPartialDefinition;
    private static readonly Func<IPropertySymbol, IPropertySymbol?> _getPartialImplementationPart;

    static SymbolExtensions()
    {
        var isPartialDefinition = typeof(IPropertySymbol).GetProperty( "IsPartialDefinition" )?.GetGetMethod();

        _isPartialDefinition = isPartialDefinition == null
            ? _ => false
            : (Func<IPropertySymbol, bool>) Delegate.CreateDelegate( typeof(Func<IPropertySymbol, bool>), isPartialDefinition );

        var getPartialImplementationPart = typeof(IPropertySymbol).GetProperty( "PartialImplementationPart" )?.GetGetMethod();

        _getPartialImplementationPart = getPartialImplementationPart == null
            ? _ => null
            : (Func<IPropertySymbol, IPropertySymbol?>) Delegate.CreateDelegate(
                typeof(Func<IPropertySymbol, IPropertySymbol?>),
                getPartialImplementationPart );
    }

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
            case IMethodSymbol { IsPartialDefinition: true, PartialImplementationPart: { } partialDefinitionSymbol }:
                return GetReferenceOfShortestPath( partialDefinitionSymbol );

            case IMethodSymbol { AssociatedSymbol: { } associatedSymbol }:
                return GetReferenceOfShortestPath( symbol ) ?? GetReferenceOfShortestPath( associatedSymbol );

            // We have to use reflection here, because the properties don't exist in Roslyn 4.4, which is the only target of this project.
            case IPropertySymbol propertySymbol when _isPartialDefinition( propertySymbol ) && _getPartialImplementationPart( propertySymbol ) is { } partialDefinitionSymbol:
                return GetReferenceOfShortestPath( partialDefinitionSymbol );

            default:
                return GetReferenceOfShortestPath( symbol );
        }
    }

    public static SyntaxNode? GetPrimaryDeclarationSyntax( this ISymbol symbol ) => symbol.GetPrimarySyntaxReference()?.GetSyntax();
}