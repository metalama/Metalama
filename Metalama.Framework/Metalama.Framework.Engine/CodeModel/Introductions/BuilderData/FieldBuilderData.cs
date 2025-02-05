// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal sealed class FieldBuilderData : MemberBuilderData
{
    private readonly IntroducedRef<IField> _ref;

    public IRef<IType> Type { get; }

    public Writeability Writeability { get; }

    public RefKind RefKind { get; }

    public bool IsRequired { get; }

    public IExpression? InitializerExpression { get; }

    // Anomaly: we need the InitializerTemplate here because we need it when overriding the property.
    public TemplateMember<IField>? InitializerTemplate { get; }

    public TypedConstantRef? ConstantValue { get; }

    public MethodBuilderData GetMethod { get; }

    public MethodBuilderData SetMethod { get; }

    public IFullRef<IProperty>? OverridingProperty { get; }

    public FieldBuilderData( FieldBuilder builder, IFullRef<INamedType> containingDeclaration ) : base( builder, containingDeclaration )
    {
        this._ref = builder.Ref;

        this.Type = builder.Type.ToRef();
        this.Writeability = builder.Writeability;
        this.RefKind = builder.RefKind;
        this.IsRequired = builder.IsRequired;
        this.InitializerExpression = builder.InitializerExpression;
        this.InitializerTemplate = builder.InitializerTemplate;
        this.ConstantValue = builder.ConstantValue.ToRef();
        this.GetMethod = new MethodBuilderData( builder.GetMethod, this._ref );
        this.SetMethod = new MethodBuilderData( builder.SetMethod, this._ref );

        this.OverridingProperty = builder.OverridingProperty?.ToFullRef();
        this.Attributes = builder.Attributes.ToImmutable( this._ref );
    }

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => this._ref;

    public new IntroducedRef<IField> ToRef() => this._ref;

    public override DeclarationKind DeclarationKind => DeclarationKind.Field;

    public override IRef<IMember>? OverriddenMember => null;
}