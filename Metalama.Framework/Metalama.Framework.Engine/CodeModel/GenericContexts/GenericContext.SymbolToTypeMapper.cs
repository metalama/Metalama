// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.GenericContexts;

public partial class GenericContext
{
    private sealed class SymbolToTypeMapper : SymbolVisitor<IType>
    {
        private readonly GenericContext _parent;
        private readonly CompilationModel _compilation;

        public SymbolToTypeMapper( GenericContext parent, CompilationModel compilation )
        {
            this._parent = parent;
            this._compilation = compilation;
        }

        public override IType DefaultVisit( ISymbol symbol ) => throw new AssertionFailedException( $"Unexpected symbol kind: '{symbol.Kind}'." );

        public override IType VisitArrayType( IArrayTypeSymbol symbol ) => this.Visit( symbol.ElementType )!.MakeArrayType( symbol.Rank );

        public override IType VisitDynamicType( IDynamicTypeSymbol symbol ) => this._compilation.Factory.GetDynamicType( this._parent.TranslateSymbolIfNecessary( symbol ) );

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
                namedType = this._compilation.Factory.GetNamedType( this._parent.TranslateSymbolIfNecessary( symbol.OriginalDefinition ) );
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

        public override IType VisitTypeParameter( ITypeParameterSymbol symbol ) => this._parent.Map( symbol, this._compilation );
    }
}