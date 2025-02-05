// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Visitors;
using Microsoft.CodeAnalysis;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Source.ConstructedTypes
{
    internal sealed class DynamicType : SymbolConstructedType<IDynamicTypeSymbol>, IDynamicType
    {
        internal DynamicType( IDynamicTypeSymbol typeSymbol, CompilationModel compilation ) : base( typeSymbol, compilation, GenericContext.Empty ) { }

        public override TypeKind TypeKind => TypeKind.Dynamic;

        public new IDynamicType ToNullable() => (IDynamicType) this.Compilation.Factory.MakeNullableType( this, true );

        public new IDynamicType ToNonNullable() => (IDynamicType) this.Compilation.Factory.MakeNullableType( this, false );

        public override IType Accept( TypeRewriter visitor ) => visitor.Visit( this );
    }
}