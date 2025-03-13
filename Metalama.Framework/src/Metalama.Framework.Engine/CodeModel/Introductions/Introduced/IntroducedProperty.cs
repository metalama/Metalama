// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.RunTime;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal sealed class IntroducedProperty : IntroducedPropertyOrIndexer, IPropertyImpl
{
    private readonly PropertyBuilderData _propertyBuilderData;

    public IntroducedProperty( PropertyBuilderData builderData, CompilationModel compilation, IGenericContext genericContext )
        : base( compilation, genericContext )
    {
        this._propertyBuilderData = builderData;
    }

    public override DeclarationBuilderData BuilderData => this._propertyBuilderData;

    protected override NamedDeclarationBuilderData NamedDeclarationBuilderData => this._propertyBuilderData;

    protected override MemberOrNamedTypeBuilderData MemberOrNamedTypeBuilderData => this._propertyBuilderData;

    protected override MemberBuilderData MemberBuilderData => this._propertyBuilderData;

    public override bool IsExplicitInterfaceImplementation => this.ExplicitInterfaceImplementations.Count > 0;

    protected override PropertyOrIndexerBuilderData PropertyOrIndexerBuilderData => this._propertyBuilderData;

    public bool? IsAutoPropertyOrField => this._propertyBuilderData.IsAutoPropertyOrField;

    [Memo]
    public IProperty? OverriddenProperty => this.MapDeclaration( this._propertyBuilderData.OverriddenProperty );

    [Memo]
    public IProperty Definition => this.Compilation.Factory.GetProperty( this._propertyBuilderData ).AssertNotNull();

    protected override IMemberOrNamedType GetDefinition() => this.Definition;

    [Memo]
    private IFullRef<IProperty> Ref => this.RefFactory.FromIntroducedDeclaration<IProperty>( this );

    IRef<IProperty> IProperty.ToRef() => this.Ref;

    protected override IFullRef<IMember> ToMemberFullRef() => this.Ref;

    IRef<IFieldOrProperty> IFieldOrProperty.ToRef() => this.Ref;

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    // TODO: When an interface is introduced, explicit implementation should appear here.
    [Memo]
    public IReadOnlyList<IProperty> ExplicitInterfaceImplementations => this.MapDeclarationList( this._propertyBuilderData.ExplicitInterfaceImplementations );

    public FieldOrPropertyInfo ToFieldOrPropertyInfo() => CompileTimeFieldOrPropertyInfo.Create( this );

    public bool IsRequired => this._propertyBuilderData.IsRequired;

    public IExpression? InitializerExpression => this._propertyBuilderData.InitializerExpression;

    public IFieldOrPropertyInvoker With( InvokerOptions options ) => new FieldOrPropertyInvoker( this, options );

    public IFieldOrPropertyInvoker With( object? target, InvokerOptions options = default ) => new FieldOrPropertyInvoker( this, options, target );

    public ref object? Value => ref new FieldOrPropertyInvoker( this ).Value;

    bool IExpression.IsAssignable => this.Writeability != Writeability.None;

    public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
        => new FieldOrPropertyInvoker( this )
            .ToTypedExpressionSyntax( syntaxGenerationContext, targetType );

    [Memo]
    public IField? OriginalField => this.GetOriginalField();

    private IField? GetOriginalField()
    {
        using ( StackOverflowHelper.Detect() )
        {
            return this.MapDeclaration( this._propertyBuilderData.OriginalField );
        }
    }
}