// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal sealed class IntroducedAttribute : IntroducedDeclaration, IAttribute
{
    private readonly AttributeBuilderData _builderData;

    public IntroducedAttribute( AttributeBuilderData builder, CompilationModel compilation, IGenericContext genericContext ) : base(
        compilation,
        genericContext )
    {
        this._builderData = builder;
    }

    IDeclaration IAttribute.ContainingDeclaration => this.ContainingDeclaration.AssertNotNull();

    [Memo]
    private AttributeRef Ref => this._builderData.ToRef();

    public IRef<IAttribute> ToRef() => this.Ref;

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => throw new NotSupportedException();

    public override DeclarationBuilderData BuilderData => this._builderData;

    [Memo]
    public INamedType Type => this.Constructor.DeclaringType;

    [Memo]
    public IConstructor Constructor => this.MapDeclaration( this._builderData.Constructor ).AssertNotNull();

    [Memo]
    public ImmutableArray<TypedConstant> ConstructorArguments
        => this._builderData.ConstructorArguments.Select( a => a.ToTypedConstant( this.Compilation ) )
            .ToImmutableArray();

    [Memo]
    public INamedArgumentList NamedArguments
        => new NamedArgumentList(
            this._builderData.NamedArguments.SelectAsArray(
                a => new KeyValuePair<string, TypedConstant>(
                    a.Key,
                    a.Value.ForCompilation( this.Compilation ) ) ) );

    int IAspectPredecessor.PredecessorDegree => 0;

    IRef<IDeclaration> IAspectPredecessor.TargetDeclaration => this._builderData.ContainingDeclaration;

    ImmutableArray<AspectPredecessor> IAspectPredecessor.Predecessors => ImmutableArray<AspectPredecessor>.Empty;

    public override bool CanBeInherited => false;

    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default ) => [];
}