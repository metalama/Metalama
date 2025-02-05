// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking;

internal static class LinkerSymbolHelper
{
    [return: NotNullIfNotNull( nameof(symbol) )]
    public static ISymbol? GetCanonicalDefinition( this ISymbol? symbol )
    {
        if ( symbol is IMethodSymbol { IsGenericMethod: true, ConstructedFrom: { } genericDefinition } )
        {
            symbol = genericDefinition;
        }

        if ( symbol is IMethodSymbol { PartialDefinitionPart: { } methodPartialDefinition } )
        {
            symbol = methodPartialDefinition;
        }

#if ROSLYN_4_12_0_OR_GREATER
        if ( symbol is IPropertySymbol { PartialDefinitionPart: { } propertyPartialDefinition } )
        {
            symbol = propertyPartialDefinition;
        }
#endif

        return symbol;
    }
}