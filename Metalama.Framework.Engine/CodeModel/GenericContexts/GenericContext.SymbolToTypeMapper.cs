// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.GenericContexts;

public partial class GenericContext
{
    private sealed class SymbolToTypeMapper( GenericContext parent, CompilationModel compilation ) : SymbolVisitor<IType>
    {
        public override IType DefaultVisit( ISymbol symbol ) => throw new AssertionFailedException( $"Unexpected symbol kind: '{symbol.Kind}'." );

        public override IType VisitArrayType( IArrayTypeSymbol symbol ) => this.Visit( symbol.ElementType )!.MakeArrayType( symbol.Rank );

        public override IType VisitDynamicType( IDynamicTypeSymbol symbol ) => compilation.Factory.GetDynamicType( parent.TranslateSymbolIfNecessary( symbol ) );

        public override IType VisitNamedType( INamedTypeSymbol symbol )
        {
            INamedType namedType;

            if ( symbol.ContainingType != null )
            {
                var containingType = (INamedType) this.VisitNamedType( symbol.ContainingType );
                namedType = containingType.Types.OfName( symbol.Name ).Single( t => t.TypeParameters.Count == symbol.Arity );
            }
            else
            {
                namedType = compilation.Factory.GetNamedType( parent.TranslateSymbolIfNecessary( symbol.OriginalDefinition ) );
            }

            if ( !namedType.IsGeneric )
            {
                return namedType;
            }
            else
            {
                var typeArguments = new IType[namedType.TypeParameters.Count];

                for ( var index = 0; index < symbol.TypeArguments.Length; index++ )
                {
                    var typeArgumentSymbol = symbol.TypeArguments[index];
                    typeArguments[index] = this.Visit( typeArgumentSymbol )!;
                }

                return namedType.MakeGenericInstance( typeArguments );
            }
        }

        public override IType VisitPointerType( IPointerTypeSymbol symbol ) => this.Visit( symbol.PointedAtType )!.MakePointerType();

        public override IType VisitFunctionPointerType( IFunctionPointerTypeSymbol symbol )
            => throw new NotImplementedException( UnsupportedFeatures.FunctionPointerMapping );

        public override IType VisitTypeParameter( ITypeParameterSymbol symbol ) => parent.Map( symbol, compilation );
    }
}