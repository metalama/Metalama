// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.Introductions.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal class ConstructorBuilderData : MemberBuilderData
{
    private readonly IFullRef<IConstructor> _ref;

    public IFullRef<IConstructor>? ReplacedImplicitConstructor { get; }

    public ImmutableArray<ParameterBuilderData> Parameters { get; }

    public ConstructorInitializerKind InitializerKind { get; }

    public bool IsImplicitlyDeclared { get; }

    public ImmutableArray<(IExpression Expression, string? ParameterName)> InitializerArguments { get; }

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => this._ref;

    public new IFullRef<IConstructor> ToRef() => this._ref;

    public override DeclarationKind DeclarationKind => DeclarationKind.Constructor;

    public ConstructorBuilderData( ConstructorBuilder builder, IFullRef<IDeclaration> containingDeclaration ) : base(
        builder,
        containingDeclaration )
    {
        this._ref = builder.Ref;

        this.Parameters = builder.Parameters.ToImmutable( this._ref );
        this.ReplacedImplicitConstructor = builder.ReplacedImplicitConstructor?.ToFullRef();
        this.InitializerKind = builder.InitializerKind;
        this.InitializerArguments = ImmutableArray.ToImmutableArray( builder.InitializerArguments );
        this.Attributes = builder.Attributes.ToImmutable( this._ref );
        this.IsImplicitlyDeclared = builder.IsImplicitlyDeclared;
    }

    public override IRef<IMember>? OverriddenMember => null;

    public override IReadOnlyList<IRef<IMember>> ExplicitInterfaceImplementationMembers => [];

    public override IEnumerable<DeclarationBuilderData> GetOwnedDeclarations() => base.GetOwnedDeclarations().Concat( this.Parameters );
}