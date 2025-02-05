// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Visitors;
using Microsoft.CodeAnalysis;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Source.ConstructedTypes;

internal sealed class SymbolFunctionPointerType : SymbolConstructedType<IFunctionPointerTypeSymbol>, IFunctionPointerType
{
    public SymbolFunctionPointerType( IFunctionPointerTypeSymbol symbol, CompilationModel compilation, GenericContext genericContext ) : base(
        symbol,
        compilation,
        genericContext ) { }

    public override TypeKind TypeKind => TypeKind.FunctionPointer;

    public override IType Accept( TypeRewriter visitor ) => visitor.Visit( this );
}