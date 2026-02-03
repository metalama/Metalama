// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking;

internal static class LinkerSymbolHelper
{
    [return: NotNullIfNotNull( nameof(symbol) )]
    public static ISymbol? GetCanonicalDefinition( this ISymbol? symbol )
    {
        if ( symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol { IsGenericMethod: true, ConstructedFrom: { } genericDefinition } )
        {
            symbol = genericDefinition;
        }

        if ( symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol { PartialDefinitionPart: { } methodPartialDefinition } )
        {
            symbol = methodPartialDefinition;
        }

#if ROSLYN_4_12_0_OR_GREATER
        if ( symbol.Kind == SymbolKind.Property && symbol is IPropertySymbol { PartialDefinitionPart: { } propertyPartialDefinition } )
        {
            symbol = propertyPartialDefinition;
        }
#endif

#if ROSLYN_5_0_0_OR_GREATER
        if ( symbol.Kind == SymbolKind.Event && symbol is IEventSymbol { PartialDefinitionPart: { } eventPartialDefinition } )
        {
            symbol = eventPartialDefinition;
        }
#endif

        return symbol;
    }
}