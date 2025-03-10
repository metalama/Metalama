// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Visitors;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Source.ConstructedTypes
{
    internal sealed class SymbolArrayType : SymbolConstructedType<IArrayTypeSymbol>, IArrayType
    {
        internal SymbolArrayType( IArrayTypeSymbol typeSymbol, CompilationModel compilation, GenericContext genericContext ) : base(
            typeSymbol,
            compilation,
            genericContext )
        {
            // Array types with lower bounds or sizes specified are not supported.
            Invariant.Assert( typeSymbol.LowerBounds.IsDefault );
            Invariant.Assert( typeSymbol.Sizes.IsEmpty );

            // MDArrays with rank 1 are not supported.
            Invariant.Implies( typeSymbol.Rank == 1, typeSymbol.IsSZArray );
        }

        internal ITypeImpl WithElementType( IType elementType )
        {
            if ( ReferenceEquals( elementType, this.ElementType ) )
            {
                return this;
            }
            else if ( elementType is ISymbolBasedCompilationElement { Symbol: ITypeSymbol typeSymbol } )
            {
                var symbol =
                    this.Compilation
                        .RoslynCompilation.CreateArrayTypeSymbol( typeSymbol, this.Rank );

                return (ITypeImpl) this.Compilation.Factory.GetIType( symbol );
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override TypeKind TypeKind => TypeKind.Array;

        [Memo]
        public IType ElementType => this.Compilation.Factory.GetIType( this.Symbol.ElementType, this.GenericContextForSymbolMapping );

        public int Rank => this.Symbol.Rank;

        public new IArrayType ToNullable() => (IArrayType) this.Compilation.Factory.MakeNullableType( this, true );

        public new IArrayType ToNonNullable() => (IArrayType) this.Compilation.Factory.MakeNullableType( this, false );

        public override IType Accept( TypeRewriter visitor ) => visitor.Visit( this );
    }
}