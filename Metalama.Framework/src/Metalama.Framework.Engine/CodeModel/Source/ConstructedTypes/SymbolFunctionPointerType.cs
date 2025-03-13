// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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