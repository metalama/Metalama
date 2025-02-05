// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.GenericContexts;

public partial class GenericContext
{
    /// <summary>
    /// Determines if a typeSymbol signature contains a generic parameter.
    /// </summary>
    private sealed class TypeParameterSymbolDetector : SymbolVisitor<bool>
    {
        private TypeParameterSymbolDetector() { }

        public static TypeParameterSymbolDetector Instance { get; } = new();

        public override bool DefaultVisit( ISymbol? symbol ) => throw new NotImplementedException( $"Symbol kind not handled: {symbol?.Kind}." );

        public override bool VisitArrayType( IArrayTypeSymbol arrayTypeSymbol ) => this.Visit( arrayTypeSymbol.ElementType );

        public override bool VisitDynamicType( IDynamicTypeSymbol dynamicTypeSymbol ) => false;

        public override bool VisitNamedType( INamedTypeSymbol namedTypeSymbol )
            => namedTypeSymbol.TypeArguments.Any( this.Visit )
               || (namedTypeSymbol.ContainingType != null && this.Visit( namedTypeSymbol.ContainingType ));

        public override bool VisitPointerType( IPointerTypeSymbol pointerTypeSymbol ) => this.Visit( pointerTypeSymbol.PointedAtType );

        public override bool VisitFunctionPointerType( IFunctionPointerTypeSymbol functionPointerTypeSymbol ) => false;

        public override bool VisitTypeParameter( ITypeParameterSymbol typeSymbolParameter ) => true;
    }
}