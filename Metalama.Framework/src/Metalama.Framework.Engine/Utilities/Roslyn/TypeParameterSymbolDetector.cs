// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

internal static class TypeParameterSymbolDetector
{
    public static ISymbol? GetTypeContext( ITypeSymbol type ) => Visitor.Instance.Visit( type )?.ContainingSymbol;

    private sealed class Visitor : SymbolVisitor<ITypeParameterSymbol?>
    {
        public static Visitor Instance { get; } = new();

        private Visitor() { }

        public override ITypeParameterSymbol DefaultVisit( ISymbol symbol ) => throw new NotImplementedException();

        public override ITypeParameterSymbol? VisitArrayType( IArrayTypeSymbol symbol ) => this.Visit( symbol.ElementType );

        public override ITypeParameterSymbol? VisitDynamicType( IDynamicTypeSymbol symbol ) => null;

        public override ITypeParameterSymbol? VisitNamedType( INamedTypeSymbol symbol )
        {
            ITypeParameterSymbol? maxTypeParameter = null;

            foreach ( var typeArgument in symbol.TypeArguments )
            {
                var typeParameter = this.Visit( typeArgument );

                if ( typeParameter != null )
                {
                    if ( typeParameter.TypeParameterKind == TypeParameterKind.Method )
                    {
                        return typeParameter;
                    }
                    else
                    {
                        maxTypeParameter = typeParameter;
                    }
                }
            }

            return maxTypeParameter;
        }

        public override ITypeParameterSymbol? VisitPointerType( IPointerTypeSymbol symbol ) => this.Visit( symbol.PointedAtType );

        public override ITypeParameterSymbol? VisitFunctionPointerType( IFunctionPointerTypeSymbol symbol ) => null;

        public override ITypeParameterSymbol VisitTypeParameter( ITypeParameterSymbol symbol ) => symbol;
    }
}