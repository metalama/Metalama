// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.References;

internal static class SymbolNormalizer
{
    private static (ISymbol Symbol, GenericContext Context) GetCanonicalSymbol(
        IMethodSymbol methodSymbol,
        GenericContext genericContext,
        RefFactory refFactory )
    {
        if ( methodSymbol.PartialImplementationPart != null )
        {
            methodSymbol = methodSymbol.PartialImplementationPart;
        }

        if ( GenericContextHelper.IsCanonicalGenericMethodInstance( methodSymbol ) )
        {
            return (methodSymbol.OriginalDefinition, genericContext);
        }
        else if ( genericContext.IsEmptyOrIdentity )
        {
            return (methodSymbol, genericContext);
        }
        else
        {
            return (methodSymbol.OriginalDefinition, SymbolGenericContext.Get( methodSymbol, refFactory.CompilationContext ).Map( genericContext, refFactory ));
        }
    }

    private static (ISymbol Symbol, GenericContext Context) GetCanonicalSymbol(
        INamedTypeSymbol namedTypeSymbol,
        GenericContext genericContext,
        RefFactory refFactory )
    {
        if ( GenericContextHelper.IsCanonicalGenericTypeInstance( namedTypeSymbol ) )
        {
            var definition = namedTypeSymbol.OriginalDefinition.WithNullableAnnotation( namedTypeSymbol.NullableAnnotation );

            return (definition, genericContext);
        }
        else if ( genericContext.IsEmptyOrIdentity )
        {
            return (namedTypeSymbol, genericContext);
        }
        else
        {
            return (namedTypeSymbol.OriginalDefinition,
                    SymbolGenericContext.Get( namedTypeSymbol, refFactory.CompilationContext ).Map( genericContext, refFactory ));
        }
    }

    private static (ISymbol Symbol, GenericContext Context) GetCanonicalSymbol(
        IPropertySymbol propertySymbol,
        GenericContext genericContext )
    {
#if ROSLYN_4_12_0_OR_GREATER
        if ( propertySymbol.PartialImplementationPart != null )
        {
            propertySymbol = propertySymbol.PartialImplementationPart;
        }
#endif

        return (propertySymbol, genericContext);
    }

    public static (ISymbol Symbol, GenericContext Context) GetCanonicalSymbol( ISymbol symbol, GenericContext genericContext, RefFactory refFactory )
        => symbol.Kind switch
        {
            SymbolKind.Method => GetCanonicalSymbol( (IMethodSymbol) symbol, genericContext, refFactory ),
            SymbolKind.Property => GetCanonicalSymbol( (IPropertySymbol) symbol, genericContext ),
            SymbolKind.NamedType => GetCanonicalSymbol( (INamedTypeSymbol) symbol, genericContext, refFactory ),
            _ => (symbol, genericContext),
        };
}