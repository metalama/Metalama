// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal sealed class PropertyBuilderData : PropertyOrIndexerBuilderData
{
    private readonly IntroducedRef<IProperty> _ref;

    public ImmutableArray<IAttributeData> FieldAttributes { get; }

    public IExpression? InitializerExpression { get; }

    // Anomaly: in case of field promotion, this can be set to the initializer template of the field,
    // bacause InitializerExpression is not available when the field is introduced from a template.
    public TemplateMember<IFieldOrProperty>? InitializerTemplate { get; }

    public bool IsAutoPropertyOrField { get; }

    public IRef<IProperty>? OverriddenProperty { get; }

    public IReadOnlyList<IRef<IProperty>> ExplicitInterfaceImplementations { get; }

    public override MethodBuilderData? GetMethod { get; }

    public override MethodBuilderData? SetMethod { get; }

    public IFullRef<IField>? OriginalField { get; }

    public bool IsRequired { get; }

    public PropertyBuilderData( PropertyBuilder builder, IFullRef<INamedType> containingDeclaration ) : base( builder, containingDeclaration )
    {
        this._ref = new IntroducedRef<IProperty>( this, containingDeclaration.RefFactory );
        this.FieldAttributes = builder.FieldAttributes.ToImmutableArray();
        this.InitializerExpression = builder.InitializerExpression;
        this.InitializerTemplate = builder.InitializerTemplate;
        this.IsAutoPropertyOrField = builder.IsAutoPropertyOrField;
        this.OverriddenProperty = builder.OverriddenProperty?.ToRef();
        this.ExplicitInterfaceImplementations = builder.ExplicitInterfaceImplementations.SelectAsImmutableArray( i => i.ToRef() );
        this.IsRequired = builder.IsRequired;
        this.OriginalField = builder.OriginalField?.ToFullRef();

        if ( builder.OriginalField != null )
        {
            Invariant.Assert( builder.OriginalField.GenericContext.IsEmptyOrIdentity );
        }

        if ( builder.GetMethod != null )
        {
            this.GetMethod = new MethodBuilderData( builder.GetMethod, this._ref );
        }

        if ( builder.SetMethod != null )
        {
            this.SetMethod = new MethodBuilderData( builder.SetMethod, this._ref );
        }

        this.Attributes = builder.Attributes.ToImmutable( this._ref );
    }

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => this._ref;

    public new IntroducedRef<IProperty> ToRef() => this._ref;

    public override DeclarationKind DeclarationKind => DeclarationKind.Property;

    public override IRef<IMember>? OverriddenMember => this.OverriddenProperty;

    protected override InsertPosition GetInsertPosition()
    {
        switch ( this.OriginalField )
        {
            case null:
                return base.GetInsertPosition();

            case ISymbolRef symbolRef:
                return symbolRef.Symbol.ToInsertPosition();

            case IIntroducedRef builtDeclarationRef:
                return builtDeclarationRef.BuilderData.InsertPosition;

            default:
                throw new AssertionFailedException();
        }
    }
}