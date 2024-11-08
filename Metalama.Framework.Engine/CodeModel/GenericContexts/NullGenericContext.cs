// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel.GenericContexts;

internal sealed class NullGenericContext : GenericContext
{
    internal override GenericContextKind Kind => GenericContextKind.Null;

    internal override ImmutableArray<IFullRef<IType>> TypeArguments => ImmutableArray<IFullRef<IType>>.Empty;

    internal override IType Map( ITypeParameter typeParameter ) => typeParameter;

    protected override IType Map( ITypeParameterSymbol typeParameterSymbol, CompilationModel compilation )
        => compilation.Factory.GetIType( typeParameterSymbol );

    internal override GenericContext Map( GenericContext genericContext, RefFactory refFactory ) => Empty;

    public override bool Equals( GenericContext? other ) => other is NullGenericContext;

    protected override int GetHashCodeCore() => 0;
}