// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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