// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

internal static partial class TypeParameterDetector
{
    private sealed class TypeSymbolVisitor : SymbolVisitor<bool>
    {
        private TypeSymbolVisitor() { }

        public static TypeSymbolVisitor Instance { get; } = new();

        public override bool DefaultVisit( ISymbol symbol ) => throw new AssertionFailedException();

        public override bool VisitDynamicType( IDynamicTypeSymbol symbol ) => false;

        public override bool VisitArrayType( IArrayTypeSymbol symbol ) => this.Visit( symbol.ElementType );

        public override bool VisitNamedType( INamedTypeSymbol symbol ) => symbol.TypeArguments.Any( this.Visit );

        public override bool VisitPointerType( IPointerTypeSymbol symbol ) => this.Visit( symbol.PointedAtType );

        public override bool VisitFunctionPointerType( IFunctionPointerTypeSymbol symbol ) => false;

        public override bool VisitTypeParameter( ITypeParameterSymbol symbol ) => true;
    }
}