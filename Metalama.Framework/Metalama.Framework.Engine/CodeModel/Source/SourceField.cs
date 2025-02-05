// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
    internal sealed class SourceField : SourceMember, IFieldImpl
    {
        private readonly IFieldSymbol _symbol;

        public override DeclarationKind DeclarationKind => DeclarationKind.Field;

        public override ISymbol Symbol => this._symbol;

        public SourceField( IFieldSymbol symbol, CompilationModel compilation, GenericContext? genericContextForSymbolMapping ) : base(
            compilation,
            genericContextForSymbolMapping )
        {
            this._symbol = symbol;
        }

        [Memo]
        public IType Type => this.Compilation.Factory.GetIType( this._symbol.Type, this.GenericContextForSymbolMapping );

        public RefKind RefKind => this._symbol.RefKind.ToOurRefKind();

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

        IRef<IFieldOrProperty> IFieldOrProperty.ToRef() => this.Ref;

        IRef<IFieldOrPropertyOrIndexer> IFieldOrPropertyOrIndexer.ToRef() => this.Ref;

        public Writeability Writeability
            => this._symbol switch
            {
                { IsConst: true } => Writeability.None,
                { IsReadOnly: true } => Writeability.ConstructorOnly,
                _ => Writeability.All
            };

        public bool? IsAutoPropertyOrField => true;

        public FieldOrPropertyInfo ToFieldOrPropertyInfo() => CompileTimeFieldOrPropertyInfo.Create( this );

        public bool IsRequired => this._symbol.IsRequired;

        [Memo]
        public IExpression? InitializerExpression => this.GetInitializerExpressionCore();

        private void CheckNotPropertyBackingField()
        {
            if ( this.IsImplicitlyDeclared )
            {
                throw new InvalidOperationException(
                    $"Cannot generate run-time syntax for '{this.ToDisplayString()}' because this is an implicit property-backing field." );
            }
        }

        public IFieldOrPropertyInvoker With( InvokerOptions options )
        {
            this.CheckNotPropertyBackingField();

            return new FieldOrPropertyInvoker( this, options );
        }

        public IFieldOrPropertyInvoker With( object? target, InvokerOptions options = default )
        {
            this.CheckNotPropertyBackingField();

            return new FieldOrPropertyInvoker( this, options, target );
        }

        public ref object? Value
        {
            get
            {
                this.CheckNotPropertyBackingField();

                return ref new FieldOrPropertyInvoker( this ).Value;
            }
        }

        public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
        {
            this.CheckNotPropertyBackingField();

            return new FieldOrPropertyInvoker( this ).ToTypedExpressionSyntax( syntaxGenerationContext, targetType );
        }

        private IExpression? GetInitializerExpressionCore()
        {
            var expression = this._symbol.GetPrimaryDeclarationSyntax() switch
            {
                VariableDeclaratorSyntax variable => variable.Initializer?.Value,
                EnumMemberDeclarationSyntax enumMember => enumMember.EqualsValue?.Value,
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
        public TypedConstant? ConstantValue => this._symbol.ConstantValue != null ? TypedConstant.Create( this._symbol.ConstantValue, this.Type ) : null;

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
            => this._symbol == this._symbol.OriginalDefinition ? this : this.Compilation.Factory.GetField( this._symbol.OriginalDefinition );

        protected override IMemberOrNamedType GetDefinitionMemberOrNamedType() => this.Definition;

        [Memo]
        private IFullRef<IField> Ref => this.RefFactory.FromSymbolBasedDeclaration<IField>( this );

        private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

        protected override IRef<IMemberOrNamedType> ToMemberOrNamedTypeRef() => this.Ref;
    }
}