// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.RunTime;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal sealed class PropertyBuilder : PropertyOrIndexerBuilder, IPropertyBuilder, IPropertyImpl
{
    private readonly List<IAttributeData> _fieldAttributes;

    private readonly IntroducedRef<IProperty> _ref;

    private IExpression? _initializerExpression;

    public PropertyBuilder(
        AspectLayerInstance aspectLayerInstance,
        INamedType targetType,
        string name,
        bool hasGetter,
        bool hasSetter,
        bool isAutoProperty,
        bool hasInitOnlySetter,
        bool hasImplicitGetter,
        bool hasImplicitSetter )
        : base( aspectLayerInstance, targetType, name, hasGetter, hasSetter, hasImplicitGetter, hasImplicitSetter )
    {
        // TODO: Sanity checks.

        Invariant.Assert( hasGetter || hasSetter );
        Invariant.Assert( !(!hasSetter && hasImplicitSetter) );
        Invariant.Assert( !(!isAutoProperty && hasImplicitSetter) );

        this._ref = new IntroducedRef<IProperty>( this.Compilation.RefFactory );
        this.IsAutoPropertyOrField = isAutoProperty;
        this.HasInitOnlySetter = hasInitOnlySetter;
        this._fieldAttributes = [];
    }

    public IReadOnlyList<IAttributeData> FieldAttributes => this._fieldAttributes;

    public override Writeability Writeability
    {
        get
            => this switch
            {
                { SetMethod: null } => Writeability.None,
                { SetMethod.IsImplicitlyDeclared: true, IsAutoPropertyOrField: true } => Writeability.ConstructorOnly,
                { HasInitOnlySetter: true } => Writeability.InitOnly,
                _ => Writeability.All
            };

        set
            => this.HasInitOnlySetter = (this, value) switch
            {
                ({ SetMethod: not null }, Writeability.All) => false,
                ({ SetMethod: not null }, Writeability.InitOnly) => true,
                _ => throw new InvalidOperationException(
                    $"Writeability can only be set for non-auto properties with a setter to either {Writeability.InitOnly} or {Writeability.All}." )
            };
    }

    public bool IsAutoPropertyOrField { get; }

    bool? IFieldOrProperty.IsAutoPropertyOrField => this.IsAutoPropertyOrField;

    public IProperty? OverriddenProperty { get; set; }

    public IProperty Definition => this;

    public IField? OriginalField { get; init; }

    public override DeclarationKind DeclarationKind => DeclarationKind.Property;

    public IReadOnlyList<IProperty> ExplicitInterfaceImplementations { get; private set; } = Array.Empty<IProperty>();

    public override bool IsExplicitInterfaceImplementation => this.ExplicitInterfaceImplementations.Count > 0;

    public override IMember? OverriddenMember => this.OverriddenProperty;

    public IExpression? InitializerExpression
    {
        get => this._initializerExpression;
        set
        {
            this.CheckNotFrozen();

            this._initializerExpression = value;
        }
    }

    // Anomaly: in case of field promotion, this can be set to the initializer template of the field,
    // bacause InitializerExpression is not available when the field is introduced from a template.
    public TemplateMember<IFieldOrProperty>? InitializerTemplate { get; set; }

    public IFieldOrPropertyInvoker With( InvokerOptions options ) => new FieldOrPropertyInvoker( this, options );

    public IFieldOrPropertyInvoker With( object? target, InvokerOptions options = default ) => new FieldOrPropertyInvoker( this, options, target );

    public ref object? Value => ref new FieldOrPropertyInvoker( this ).Value;

    bool IExpression.IsAssignable => this.Writeability != Writeability.None;

    public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
        => new FieldOrPropertyInvoker( this )
            .ToTypedExpressionSyntax( syntaxGenerationContext, targetType );

    public void AddFieldAttribute( IAttributeData attributeData ) => this._fieldAttributes.Add( attributeData );

    public FieldOrPropertyInfo ToFieldOrPropertyInfo() => CompileTimeFieldOrPropertyInfo.Create( this );

    public bool IsRequired { get; set; }

    public void SetExplicitInterfaceImplementation( IProperty interfaceProperty ) => this.ExplicitInterfaceImplementations = [interfaceProperty];

    IRef<IProperty> IProperty.ToRef() => this._ref;

    protected override IFullRef<IMember> ToMemberFullRef() => this._ref;

    protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this._ref;

    IRef<IFieldOrProperty> IFieldOrProperty.ToRef() => this._ref;

    public new IFullRef<IProperty> ToRef() => this._ref;

    protected override void EnsureReferenceInitialized()
    {
        this._ref.BuilderData = new PropertyBuilderData( this.AssertFrozen(), this.DeclaringType.ToFullRef() );
    }

    public PropertyBuilderData BuilderData => (PropertyBuilderData) this._ref.BuilderData;

    public bool? IsDesignTimeObservableOverride { get; init; }

    public override bool IsDesignTimeObservable => this.IsDesignTimeObservableOverride ?? base.IsDesignTimeObservable;

    public override SyntaxTree? PrimarySyntaxTree => this.OriginalField?.GetPrimarySyntaxTree() ?? base.PrimarySyntaxTree;
}