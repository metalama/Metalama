// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Source.Pseudo;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.RunTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Reflection;
using Accessibility = Metalama.Framework.Code.Accessibility;
using MethodKind = Metalama.Framework.Code.MethodKind;
using RefKind = Metalama.Framework.Code.RefKind;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.CodeModel.Source
{
    internal class SourceField : SourceMember, IFieldImpl
    {
        public IFieldSymbol FieldSymbol { get; }

        public override DeclarationKind DeclarationKind => DeclarationKind.Field;

        public override ISymbol Symbol => this.FieldSymbol;

        public SourceField( IFieldSymbol symbol, CompilationModel compilation, GenericContext? genericContextForSymbolMapping ) : base(
            compilation,
            genericContextForSymbolMapping )
        {
            this.FieldSymbol = symbol;
        }

        [Memo]
        public IType Type => this.Compilation.Factory.GetIType( this.FieldSymbol.Type, this.GenericContextForSymbolMapping, defaultNullability: null );

        public RefKind RefKind => this.FieldSymbol.RefKind.ToOurRefKind();

        [Memo]
        public IMethod GetMethod => new PseudoGetter( this );

        [Memo]
        public IMethod? SetMethod
            => this.Writeability switch
            {
                Writeability.None => null,
                Writeability.ConstructorOnly => new PseudoSetter( this, Accessibility.Private ),
                Writeability.All => new PseudoSetter( this, null ),
                _ => throw new AssertionFailedException( $"Unexpected Writeability: {this.Writeability}." )
            };

        public IRef<IField> ToRef() => this.Ref;

        // Intentionally no cached with [Memo] because it can be changed by promoting the field.
        public IProperty? OverridingProperty => FieldHelper.GetOverridingProperty( this );

        public virtual FieldKind FieldKind => FieldKind.Default;

        IRef<IFieldOrProperty> IFieldOrProperty.ToRef() => this.Ref;

        IRef<IFieldOrPropertyOrIndexer> IFieldOrPropertyOrIndexer.ToRef() => this.Ref;

        public Writeability Writeability
            => this.FieldSymbol switch
            {
                { IsConst: true } => Writeability.None,
                { IsReadOnly: true } => Writeability.ConstructorOnly,
                _ => Writeability.All
            };

        public bool? IsAutoPropertyOrField => true;

        public FieldOrPropertyInfo ToFieldOrPropertyInfo() => CompileTimeFieldOrPropertyInfo.Create( this );

        public bool IsRequired => this.FieldSymbol.IsRequired;

        [Memo]
        public IExpression? InitializerExpression => this.GetInitializerExpressionCore();

        private void CheckNotPropertyBackingField()
        {
            if ( this.IsImplicitlyDeclared && this.FieldKind != FieldKind.TupleElement )
            {
                throw new InvalidOperationException(
                    $"Cannot generate run-time syntax for '{this.ToDisplayString()}' because this is an implicit property-backing field." );
            }
        }

        [Memo]
        private IFieldOrPropertyInvoker Invoker => this.CreateInvoker();

        protected virtual IFieldOrPropertyInvoker CreateInvoker()
        {
            this.CheckNotPropertyBackingField();

            return new FieldOrPropertyInvoker( this );
        }

        IFieldOrPropertyInvoker IFieldOrPropertyInvoker.WithOptions( InvokerOptions options ) => this.Invoker.WithOptions( options );

        IFieldOrPropertyInvoker IFieldOrPropertyInvoker.WithObject( object? target ) => this.Invoker.WithObject( target );

        IFieldOrPropertyInvoker IFieldOrPropertyInvoker.WithObject( IExpression? target ) => this.Invoker.WithObject( target );

        IFieldOrPropertyInvoker IFieldOrPropertyInvoker.With( InvokerOptions options ) => this.Invoker.WithOptions( options );

        IFieldOrPropertyInvoker IFieldOrPropertyInvoker.With( object? target, InvokerOptions options )
            => this.Invoker.WithOptions( options ).WithObject( target );

        ref object? IExpression.Value => ref this.Invoker.Value;

        public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
        {
            this.CheckNotPropertyBackingField();

            return this.Invoker.ToTypedExpressionSyntax( syntaxGenerationContext, targetType );
        }

        private IExpression? GetInitializerExpressionCore()
        {
            var declarationSyntax = this.FieldSymbol.GetPrimaryDeclarationSyntax();

            var expression = declarationSyntax?.Kind() switch
            {
                SyntaxKind.VariableDeclarator when declarationSyntax is VariableDeclaratorSyntax variable => variable.Initializer?.Value,
                SyntaxKind.EnumMemberDeclaration when declarationSyntax is EnumMemberDeclarationSyntax enumMember => enumMember.EqualsValue?.Value,
                _ => null
            };

            if ( expression == null )
            {
                return null;
            }
            else
            {
                return new SourceUserExpression( expression, this.Type );
            }
        }

        public FieldInfo ToFieldInfo() => CompileTimeFieldInfo.Create( this );

        [Memo]
        public TypedConstant? ConstantValue
            => this.FieldSymbol.ConstantValue != null ? TypedConstant.Create( this.FieldSymbol.ConstantValue, this.Type ) : null;

        public override bool IsExplicitInterfaceImplementation => false;

        protected override IRef<IMember> ToMemberRef() => this.Ref;

        public override bool IsAsync => false;

        public override IMember? OverriddenMember => null;

        public override MemberInfo ToMemberInfo() => this.ToFieldOrPropertyInfo();

        public IMethod? GetAccessor( MethodKind methodKind )
            => methodKind switch
            {
                MethodKind.PropertyGet => this.GetMethod,
                MethodKind.PropertySet => this.SetMethod,
                _ => throw new ArgumentOutOfRangeException()
            };

        public IEnumerable<IMethod> Accessors
        {
            get
            {
                yield return this.GetMethod;

                if ( this.SetMethod != null )
                {
                    yield return this.SetMethod;
                }
            }
        }

        bool IExpression.IsAssignable => this.Writeability != Writeability.None;

        [Memo]
        public IField Definition
            => this.FieldSymbol == this.FieldSymbol.OriginalDefinition ? this : this.Compilation.Factory.GetField( this.FieldSymbol.OriginalDefinition );

        protected override IMemberOrNamedType GetDefinitionMemberOrNamedType() => this.Definition;

        [Memo]
        protected virtual IFullRef<IField> Ref => this.RefFactory.FromSymbolBasedDeclaration<IField>( this );

        private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

        protected override IRef<IMemberOrNamedType> ToMemberOrNamedTypeRef() => this.Ref;
    }
}