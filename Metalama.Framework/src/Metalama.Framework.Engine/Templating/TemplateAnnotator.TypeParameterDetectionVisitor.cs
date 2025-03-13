// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.Templating;

internal sealed partial class TemplateAnnotator
{
    /// <summary>
    /// Determines if a type references a run-time template type parameter.
    /// </summary>
    private sealed class TypeParameterDetectionVisitor : SymbolVisitor<bool>
    {
        private readonly TemplateAnnotator _parent;

        public TypeParameterDetectionVisitor( TemplateAnnotator parent )
        {
            this._parent = parent;
        }

        public override bool DefaultVisit( ISymbol symbol ) => throw new AssertionFailedException( $"The visitor has not been implemented for {symbol.Kind}." );

        public override bool VisitArrayType( IArrayTypeSymbol symbol ) => this.Visit( symbol.ElementType );

        public override bool VisitDynamicType( IDynamicTypeSymbol symbol ) => false;

        public override bool VisitNamedType( INamedTypeSymbol symbol ) => symbol.TypeArguments.Any( this.Visit );

        public override bool VisitPointerType( IPointerTypeSymbol symbol ) => this.Visit( symbol.PointedAtType );

        public override bool VisitFunctionPointerType( IFunctionPointerTypeSymbol symbol )
            => this.Visit( symbol.Signature.ReturnType ) ||
               symbol.Signature.Parameters.Any( p => this.Visit( p.Type ) );

        public override bool VisitTypeParameter( ITypeParameterSymbol symbol )
            => this._parent._templateMemberClassifier.IsRunTimeTemplateTypeParameter( symbol );
    }
}