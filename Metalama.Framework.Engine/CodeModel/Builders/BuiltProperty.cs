// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.RunTime;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Builders;

internal sealed class BuiltProperty : BuiltPropertyOrIndexer, IPropertyImpl
{
    public BuiltProperty( PropertyBuilder builder, CompilationModel compilation ) : base( builder, compilation ) { }

    public PropertyBuilder PropertyBuilder => (PropertyBuilder) this.MemberBuilder;

    public bool? IsAutoPropertyOrField => this.PropertyBuilder.IsAutoPropertyOrField;

    [Memo]
    public IProperty? OverriddenProperty => this.Compilation.Factory.GetDeclaration( this.PropertyBuilder.OverriddenProperty );

    IProperty IProperty.Definition => this;

    public override SyntaxTree? PrimarySyntaxTree =>
        this.Builder switch
        {
            PromotedField promotedField => promotedField.Field.PrimarySyntaxTree,
            _ => base.PrimarySyntaxTree,
        };

    // TODO: When an interface is introduced, explicit implementation should appear here.
    [Memo]
    public IReadOnlyList<IProperty> ExplicitInterfaceImplementations
        => this.PropertyBuilder.ExplicitInterfaceImplementations.SelectAsImmutableArray( i => this.Compilation.Factory.GetDeclaration( i ) );

    public FieldOrPropertyInfo ToFieldOrPropertyInfo() => this.PropertyBuilder.ToFieldOrPropertyInfo();

    public bool IsRequired => this.PropertyBuilder.IsRequired;

    public IExpression? InitializerExpression => this.PropertyBuilder.InitializerExpression;

    public IFieldOrPropertyInvoker With( InvokerOptions options ) => this.PropertyBuilder.With( options );

    public IFieldOrPropertyInvoker With( object? target, InvokerOptions options = default ) => this.PropertyBuilder.With( target, options );

    public ref object? Value => ref this.PropertyBuilder.Value;

    public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext )
        => this.PropertyBuilder.ToTypedExpressionSyntax( syntaxGenerationContext );

    bool IExpression.IsAssignable => this.Writeability != Writeability.None;
}