// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.Introductions.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal sealed class ConstructorBuilderData : MemberBuilderData
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
        this.InitializerArguments = builder.InitializerArguments.ToImmutableArray();
        this.Attributes = builder.Attributes.ToImmutable( this._ref );
        this.IsImplicitlyDeclared = builder.IsImplicitlyDeclared;
    }

    public override IRef<IMember>? OverriddenMember => null;

    public override IEnumerable<DeclarationBuilderData> GetOwnedDeclarations() => base.GetOwnedDeclarations().Concat( this.Parameters );
}