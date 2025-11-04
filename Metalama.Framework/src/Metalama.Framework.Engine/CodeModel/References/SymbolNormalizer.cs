// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.References;

internal static class SymbolNormalizer
{
    private static CanonicalSymbolInfo GetCanonicalSymbolInfo(
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
            return new CanonicalSymbolInfo( methodSymbol.OriginalDefinition, genericContext );
        }
        else if ( genericContext.IsEmptyOrIdentity )
        {
            return new CanonicalSymbolInfo( methodSymbol, genericContext );
        }
        else
        {
            return new CanonicalSymbolInfo(
                methodSymbol.OriginalDefinition,
                SymbolGenericContext.Get( methodSymbol, refFactory.CompilationContext ).Map( genericContext, refFactory ) );
        }
    }

    private static CanonicalSymbolInfo GetCanonicalSymbolInfo(
        INamedTypeSymbol namedTypeSymbol,
        GenericContext genericContext,
        RefFactory refFactory )
    {
        if ( namedTypeSymbol.IsExtensionSafe() )
        {
            return new CanonicalSymbolInfo( namedTypeSymbol, GenericContext.Empty );
        }
        else
        {
            if ( GenericContextHelper.IsCanonicalGenericTypeInstance( namedTypeSymbol ) )
            {
                var definition = namedTypeSymbol.IsTupleType
                    ? namedTypeSymbol
                    : namedTypeSymbol.OriginalDefinition.WithNullableAnnotation( namedTypeSymbol.NullableAnnotation );

                return new CanonicalSymbolInfo( definition, genericContext );
            }
            else if ( genericContext.IsEmptyOrIdentity )
            {
                return new CanonicalSymbolInfo( namedTypeSymbol, genericContext );
            }
            else
            {
                return new CanonicalSymbolInfo(
                    namedTypeSymbol.OriginalDefinition,
                    SymbolGenericContext.Get( namedTypeSymbol, refFactory.CompilationContext ).Map( genericContext, refFactory ) );
            }
        }
    }

    private static CanonicalSymbolInfo GetCanonicalSymbolInfo(
        IPropertySymbol propertySymbol,
        GenericContext genericContext )
    {
#if ROSLYN_4_12_0_OR_GREATER
        if ( propertySymbol.PartialImplementationPart != null )
        {
            propertySymbol = propertySymbol.PartialImplementationPart;
        }
#endif

        return new CanonicalSymbolInfo( propertySymbol, genericContext );
    }

    private static CanonicalSymbolInfo GetCanonicalSymbolInfo(
        IEventSymbol eventSymbol,
        GenericContext genericContext )
    {
#if ROSLYN_5_0_0_OR_GREATER
        if ( eventSymbol.PartialImplementationPart != null )
        {
            eventSymbol = eventSymbol.PartialImplementationPart;
        }
#endif

        return new CanonicalSymbolInfo( eventSymbol, genericContext );
    }

    public static CanonicalSymbolInfo GetCanonicalSymbolInfo( ISymbol symbol, GenericContext genericContext, RefFactory refFactory )
        => symbol.Kind switch
        {
            SymbolKind.Method => GetCanonicalSymbolInfo( (IMethodSymbol) symbol, genericContext, refFactory ),
            SymbolKind.Property => GetCanonicalSymbolInfo( (IPropertySymbol) symbol, genericContext ),
            SymbolKind.Event => GetCanonicalSymbolInfo( (IEventSymbol) symbol, genericContext ),
            SymbolKind.NamedType => GetCanonicalSymbolInfo( (INamedTypeSymbol) symbol, genericContext, refFactory ),
            _ => new CanonicalSymbolInfo( symbol, genericContext )
        };

    public record struct CanonicalSymbolInfo( ISymbol Symbol, GenericContext Context );
}