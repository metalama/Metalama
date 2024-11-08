// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

internal sealed class IntroducedProperty( PropertyBuilderData builderData, CompilationModel compilation, IGenericContext genericContext )
    : IntroducedPropertyOrIndexer( compilation, genericContext ), IPropertyImpl
{
    public override DeclarationBuilderData BuilderData => builderData;

    protected override NamedDeclarationBuilderData NamedDeclarationBuilderData => builderData;

    protected override MemberOrNamedTypeBuilderData MemberOrNamedTypeBuilderData => builderData;

    protected override MemberBuilderData MemberBuilderData => builderData;

    public override bool IsExplicitInterfaceImplementation => this.ExplicitInterfaceImplementations.Count > 0;

    protected override PropertyOrIndexerBuilderData PropertyOrIndexerBuilderData => builderData;

    public bool? IsAutoPropertyOrField => builderData.IsAutoPropertyOrField;

    [Memo]
    public IProperty? OverriddenProperty => this.MapDeclaration( builderData.OverriddenProperty );

    [Memo]
    public IProperty Definition => this.Compilation.Factory.GetProperty( builderData ).AssertNotNull();

    protected override IMemberOrNamedType GetDefinition() => this.Definition;

    [Memo]
    private IFullRef<IProperty> Ref => this.RefFactory.FromIntroducedDeclaration<IProperty>( this );

    IRef<IProperty> IProperty.ToRef() => this.Ref;

    protected override IFullRef<IMember> ToMemberFullRef() => this.Ref;

    IRef<IFieldOrProperty> IFieldOrProperty.ToRef() => this.Ref;

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    // TODO: When an interface is introduced, explicit implementation should appear here.
    [Memo]
    public IReadOnlyList<IProperty> ExplicitInterfaceImplementations => this.MapDeclarationList( builderData.ExplicitInterfaceImplementations );

    public FieldOrPropertyInfo ToFieldOrPropertyInfo() => CompileTimeFieldOrPropertyInfo.Create( this );

    public bool IsRequired => builderData.IsRequired;

    public IExpression? InitializerExpression => builderData.InitializerExpression;

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
            return this.MapDeclaration( builderData.OriginalField );
        }
    }
}