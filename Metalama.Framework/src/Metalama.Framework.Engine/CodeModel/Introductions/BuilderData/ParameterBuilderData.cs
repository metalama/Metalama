// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal sealed class ParameterBuilderData : NamedDeclarationBuilderData
{
    private readonly IntroducedRef<IParameter> _ref;

    public IFullRef<IType> Type { get; }

    public RefKind RefKind { get; }

    public int Index { get; }

    public TypedConstantRef? DefaultValue { get; }

    public bool IsParams { get; }

    public bool IsThis { get; }

    public ParameterBuilderData( BaseParameterBuilder builder, IFullRef<IDeclaration> containingDeclaration ) : base( builder, containingDeclaration )
    {
        Invariant.Assert( builder.Type is not { TypeKind: TypeKind.Dynamic } );

        this._ref = builder.Ref;

        this.Type = builder.Type.ToFullRef();
        this.RefKind = builder.RefKind;
        this.Index = builder.Index;
        this.DefaultValue = builder.DefaultValue.ToRef();
        this.IsParams = builder.IsParams;
        this.IsThis = builder.IsThis;
        this.Attributes = builder.Attributes.ToImmutable( this._ref );
    }

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => this._ref;

    public override IFullRef<INamedType>? DeclaringType => this.ContainingDeclaration.DeclaringType;

    public new IFullRef<IParameter> ToRef() => this._ref;

    public override DeclarationKind DeclarationKind => DeclarationKind.Parameter;

    public override string ToString() => this.ContainingDeclaration + "/" + this.Name;
}