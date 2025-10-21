// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

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
            var tupleElementNames = 0;

            if ( namedTypeSymbol.IsTupleType )
            {
                HashCode hasher = default;

                foreach ( var tupleElement in namedTypeSymbol.TupleElements )
                {
                    hasher.Add( tupleElement.Name );
                }

                tupleElementNames = hasher.ToHashCode();
            }

            if ( GenericContextHelper.IsCanonicalGenericTypeInstance( namedTypeSymbol ) )
            {
                var definition = namedTypeSymbol.IsTupleType
                    ? namedTypeSymbol
                    : namedTypeSymbol.OriginalDefinition.WithNullableAnnotation( namedTypeSymbol.NullableAnnotation );

                return new CanonicalSymbolInfo( definition, genericContext, tupleElementNames );
            }
            else if ( genericContext.IsEmptyOrIdentity )
            {
                return new CanonicalSymbolInfo( namedTypeSymbol, genericContext, tupleElementNames );
            }
            else
            {
                return new CanonicalSymbolInfo(
                    namedTypeSymbol.OriginalDefinition,
                    SymbolGenericContext.Get( namedTypeSymbol, refFactory.CompilationContext ).Map( genericContext, refFactory ),
                    tupleElementNames );
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

    public static CanonicalSymbolInfo GetCanonicalSymbolInfo( ISymbol symbol, GenericContext genericContext, RefFactory refFactory )
        => symbol.Kind switch
        {
            SymbolKind.Method => GetCanonicalSymbolInfo( (IMethodSymbol) symbol, genericContext, refFactory ),
            SymbolKind.Property => GetCanonicalSymbolInfo( (IPropertySymbol) symbol, genericContext ),
            SymbolKind.NamedType => GetCanonicalSymbolInfo( (INamedTypeSymbol) symbol, genericContext, refFactory ),
            _ => new CanonicalSymbolInfo( symbol, genericContext )
        };

    public record struct CanonicalSymbolInfo( ISymbol Symbol, GenericContext Context, int AdditionalSymbolHash = 0 )
    {
        public CanonicalSymbolKey ToKey() => new( this.Symbol, this.AdditionalSymbolHash );
    }

    public record struct CanonicalSymbolKey( ISymbol Symbol, int AdditionalSymbolHash );

    public class CanonicalSymbolKeyComparer : IEqualityComparer<CanonicalSymbolKey>
    {
        private readonly IEqualityComparer<ISymbol> _underlyingSymbolComparer;

        public CanonicalSymbolKeyComparer( IEqualityComparer<ISymbol> underlyingSymbolComparer )
        {
            this._underlyingSymbolComparer = underlyingSymbolComparer;
        }

        public bool Equals( CanonicalSymbolKey x, CanonicalSymbolKey y )
        {
            return x.AdditionalSymbolHash == y.AdditionalSymbolHash && this._underlyingSymbolComparer.Equals( x.Symbol, y.Symbol );
        }

        public int GetHashCode( CanonicalSymbolKey obj )
        {
            return HashCode.Combine( this._underlyingSymbolComparer.GetHashCode( obj.Symbol ), obj.AdditionalSymbolHash );
        }
    }
}