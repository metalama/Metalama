// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.RunTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#if ROSLYN_4_12_0_OR_GREATER
using System.Collections.Immutable;
#endif

namespace Metalama.Framework.Engine.CodeModel.Source
{
    internal sealed class SourceProperty : SourcePropertyOrIndexer, IPropertyImpl
    {
        public SourceProperty( IPropertySymbol symbol, CompilationModel compilation, GenericContext? genericContextForSymbolMapping ) : base(
            symbol,
            compilation,
            genericContextForSymbolMapping ) 
        {
#if ROSLYN_4_12_0_OR_GREATER
            Invariant.Assert(
                symbol.PartialDefinitionPart == null,
                "Cannot use partial implementation to instantiate the SourceProperty class." );
#endif
        }

        public FieldOrPropertyInfo ToFieldOrPropertyInfo() => CompileTimeFieldOrPropertyInfo.Create( this );

        public override MemberInfo ToMemberInfo() => this.ToFieldOrPropertyInfo();

        public bool IsRequired => this.PropertySymbol.IsRequired;

        [Memo]
        public bool? IsAutoPropertyOrField => this.PropertySymbol.IsAutoProperty();

        public IProperty? OverriddenProperty
        {
            get
            {
                var overriddenProperty = this.PropertySymbol.OverriddenProperty;

                if ( overriddenProperty != null )
                {
                    return this.Compilation.Factory.GetProperty( overriddenProperty, this.GenericContextForSymbolMapping );
                }
                else
                {
                    return null;
                }
            }
        }

        [Memo]
        public IProperty Definition
            => this.PropertySymbol == this.PropertySymbol.OriginalDefinition
                ? this
                : this.Compilation.Factory.GetProperty( this.PropertySymbol.OriginalDefinition );

#if ROSLYN_4_12_0_OR_GREATER
        public override bool IsPartial => this.PropertySymbol.IsPartialDefinition || this.PropertySymbol.PartialDefinitionPart != null;
#endif

        IRef<IFieldOrProperty> IFieldOrProperty.ToRef() => this.Ref;

        IField? IProperty.OriginalField => null;

        protected override IMemberOrNamedType GetDefinitionMemberOrNamedType() => this.Definition;

        public override IMember? OverriddenMember => this.OverriddenProperty;

        [Memo]
        public IReadOnlyList<IProperty> ExplicitInterfaceImplementations
            => this.PropertySymbol.ExplicitInterfaceImplementations
                .Select( p => this.Compilation.Factory.GetProperty( p, this.GenericContextForSymbolMapping ) )
                .ToReadOnlyList();

        public override DeclarationKind DeclarationKind => DeclarationKind.Property;

        [Memo]
        public IExpression? InitializerExpression => this.GetInitializerExpressionCore();

        public IFieldOrPropertyInvoker With( InvokerOptions options ) => new FieldOrPropertyInvoker( this, options );

        public IFieldOrPropertyInvoker With( object? target, InvokerOptions options = default ) => new FieldOrPropertyInvoker( this, options, target );

        public ref object? Value => ref new FieldOrPropertyInvoker( this ).Value;

        public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
            => new FieldOrPropertyInvoker( this )
                .ToTypedExpressionSyntax( syntaxGenerationContext, targetType );

        private IExpression? GetInitializerExpressionCore()
        {
            // The declaration for properties created from record primary constructor parameters is ParameterSyntax.
            // Since those don't have a normal initializer, ignore them here.
            var initializer = (this.PropertySymbol.GetPrimaryDeclarationSyntax() as PropertyDeclarationSyntax)?.Initializer;

            if ( initializer == null )
            {
                return null;
            }
            else
            {
                return new SourceUserExpression( initializer.Value, this.Type );
            }
        }

        bool IExpression.IsAssignable => this.Writeability != Writeability.None;

#if ROSLYN_4_12_0_OR_GREATER
        [Memo]
        public override ImmutableArray<SourceReference> Sources => this.GetSourcesImpl();

        private ImmutableArray<SourceReference> GetSourcesImpl()
        {
            if ( this.PropertySymbol.PartialImplementationPart != null )
            {
                var sources = ImmutableArray.CreateBuilder<SourceReference>( 2 );
                sources.Add( new SourceReference( this.PropertySymbol.DeclaringSyntaxReferences[0].GetSyntax(), SourceReferenceImpl.Instance ) );

                sources.Add(
                    new SourceReference( this.PropertySymbol.PartialImplementationPart.DeclaringSyntaxReferences[0].GetSyntax(), SourceReferenceImpl.Instance ) );

                return sources.MoveToImmutable();
            }
            else
            {
                return base.Sources;
            }
        }
#endif

        [Memo]
        private IFullRef<IProperty> Ref => this.RefFactory.FromSymbolBasedDeclaration<IProperty>( this );

        private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

        IRef<IProperty> IProperty.ToRef() => this.Ref;

        protected override IRef<IPropertyOrIndexer> ToPropertyOrIndexerRef() => this.Ref;

        protected override IRef<IFieldOrPropertyOrIndexer> ToFieldOrPropertyOrIndexerRef() => this.Ref;

        protected override IRef<IMember> ToMemberRef() => this.Ref;

        protected override IRef<IMemberOrNamedType> ToMemberOrNamedTypeRef() => this.Ref;
    }
}