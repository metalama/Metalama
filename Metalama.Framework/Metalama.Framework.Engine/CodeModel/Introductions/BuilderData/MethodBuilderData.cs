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

internal sealed class MethodBuilderData : MemberBuilderData
{
    private readonly IFullRef<IMethod> _ref;

    public IReadOnlyList<IRef<IMethod>> ExplicitInterfaceImplementations { get; }

    public bool IsReadOnly { get; }

    public bool IsIteratorMethod { get; }

    public ImmutableArray<TypeParameterBuilderData> TypeParameters { get; }

    public override DeclarationKind DeclarationKind { get; }

    public ParameterBuilderData ReturnParameter { get; }

    public ImmutableArray<ParameterBuilderData> Parameters { get; }

    public IRef<IMethod>? OverriddenMethod { get; }

    public override IRef<IMember>? OverriddenMember => this.OverriddenMethod;

    public MethodKind MethodKind { get; }

    public OperatorKind OperatorKind { get; }

    public MethodBuilderData( IMethodBuilderImpl builder, IFullRef<IDeclaration> containingDeclaration ) : base(
        builder,
        containingDeclaration )
    {
        this.DeclarationKind = builder.DeclarationKind; // Can be Method, Finalizer, Operator.

        this._ref = builder.Ref;

        this.IsReadOnly = builder.IsReadOnly;
        this.IsIteratorMethod = builder.IsIteratorMethod.AssertNotNull();
        this.TypeParameters = builder.TypeParameters.ToImmutable( this._ref );
        this.Parameters = builder.Parameters.ToImmutable( this._ref );
        this.ReturnParameter = new ParameterBuilderData( builder.ReturnParameter, this._ref );
        this.OverriddenMethod = builder.OverriddenMethod?.ToRef();
        this.ExplicitInterfaceImplementations = builder.ExplicitInterfaceImplementations.SelectAsImmutableArray( i => i.ToRef() );
        this.MethodKind = builder.MethodKind;
        this.OperatorKind = builder.OperatorKind;
        this.Attributes = builder.Attributes.ToImmutable( this._ref );
    }

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => this._ref;

    public new IFullRef<IMethod> ToRef() => this._ref;

    public override IEnumerable<DeclarationBuilderData> GetOwnedDeclarations()
        => base.GetOwnedDeclarations().Concat( this.Parameters ).Concat( this.ReturnParameter );

    public override string ToString() => $"{this.ContainingDeclaration}.{this.Name}({string.Join( ", ", this.Parameters.Select( p => p.Type.ToString() ) )})";
}