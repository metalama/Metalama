// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RoslynMethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.CodeModel.Source
{
    internal sealed class SourceConstructor : SourceMethodBase, IConstructorImpl
    {
        public SourceConstructor( IMethodSymbol symbol, CompilationModel compilation, GenericContext? genericContextForSymbolMapping ) : base(
            symbol,
            compilation,
            genericContextForSymbolMapping )
        {
            if ( symbol.MethodKind != RoslynMethodKind.Constructor && symbol.MethodKind != RoslynMethodKind.StaticConstructor )
            {
                throw new ArgumentOutOfRangeException( nameof(symbol), "The Constructor class must be used only with constructors." );
            }
        }

        [Memo]
        private IFullRef<IConstructor> Ref => this.RefFactory.FromSymbolBasedDeclaration<IConstructor>( this );

        private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

        IRef<IConstructor> IConstructor.ToRef() => this.Ref;

        protected override IFullRef<IMethodBase> GetMethodBaseRef() => this.Ref;

        protected override IRef<IMember> ToMemberRef() => this.Ref;

        protected override IRef<IMemberOrNamedType> ToMemberOrNamedTypeRef() => this.Ref;

        [Memo]
        public ConstructorInitializerKind InitializerKind
            => this.GetPrimaryDeclarationSyntax() switch
            {
                null => ConstructorInitializerKind.None,
                ConstructorDeclarationSyntax { Initializer: null } => ConstructorInitializerKind.None,
                ConstructorDeclarationSyntax { Initializer: { } initializer } when initializer.IsKind( SyntaxKind.ThisConstructorInitializer ) =>
                    ConstructorInitializerKind.This,
                ConstructorDeclarationSyntax { Initializer: { } initializer } when initializer.IsKind( SyntaxKind.BaseConstructorInitializer ) =>
                    ConstructorInitializerKind.Base,
                TypeDeclarationSyntax { BaseList: null } =>
                    ConstructorInitializerKind.None,
                TypeDeclarationSyntax { BaseList: { } baseList } =>
                    baseList.Types.Any( bt => bt.IsKind( SyntaxKind.PrimaryConstructorBaseType ) )
                        ? ConstructorInitializerKind.Base
                        : ConstructorInitializerKind.None,
                _ => throw new AssertionFailedException( $"Unexpected initializer for '{this}'." )
            };

        public override DeclarationKind DeclarationKind => DeclarationKind.Constructor;

        public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default ) => [];

        public override bool IsExplicitInterfaceImplementation => false;

        public override bool IsAsync => false;

        [Memo]
        public bool IsPrimary => this.MethodSymbol.IsPrimaryConstructor();

        public override IMember? OverriddenMember => null;

        public IConstructor? GetBaseConstructor()
        {
            var declaration = this.GetPrimaryDeclarationSyntax();

            SyntaxNode? initializer = declaration switch
            {
                null => null,
                ConstructorDeclarationSyntax constructorDeclaration => constructorDeclaration.Initializer,
                TypeDeclarationSyntax typeDeclarationSyntax => typeDeclarationSyntax.BaseList?.Types.FirstOrDefault() as PrimaryConstructorBaseTypeSyntax,
                _ => throw new AssertionFailedException( $"Unexpected constructor syntax {declaration.GetType()}." )
            };

            if ( initializer == null )
            {
                // This is necessarily the default constructor of the base type, if any.
                return this.DeclaringType.BaseType?.Constructors.SingleOrDefault( c => c.Parameters.Count == 0 );
            }
            else
            {
                var semanticModel = this.Compilation.RoslynCompilation.GetCachedSemanticModel( declaration!.SyntaxTree );
                var symbol = (IMethodSymbol?) semanticModel.GetSymbolInfo( initializer ).Symbol;

                if ( symbol == null )
                {
                    return null;
                }
                else
                {
                    return this.Compilation.Factory.GetConstructor( symbol, this.GenericContextForSymbolMapping );
                }
            }
        }

        public ConstructorInfo ToConstructorInfo() => CompileTimeConstructorInfo.Create( this );

        [Memo]
        public IConstructor Definition
            => this.MethodSymbol == this.MethodSymbol.OriginalDefinition
                ? this
                : this.Compilation.Factory.GetConstructor( this.MethodSymbol.OriginalDefinition );

        protected override IMemberOrNamedType GetDefinitionMemberOrNamedType() => this.Definition;

        public override MethodBase ToMethodBase() => CompileTimeConstructorInfo.Create( this );

        public object Invoke( params object?[] args ) => new ConstructorInvoker( this ).Invoke( args );

        public object Invoke( IEnumerable<IExpression> args ) => new ConstructorInvoker( this ).Invoke( args );

        public IObjectCreationExpression CreateInvokeExpression() => new ConstructorInvoker( this ).CreateInvokeExpression();

        public IObjectCreationExpression CreateInvokeExpression( params object?[] args ) => new ConstructorInvoker( this ).CreateInvokeExpression( args );

        public IObjectCreationExpression CreateInvokeExpression( params IExpression[] args ) => new ConstructorInvoker( this ).CreateInvokeExpression( args );

        public IObjectCreationExpression CreateInvokeExpression( IEnumerable<IExpression> args )
            => new ConstructorInvoker( this ).CreateInvokeExpression( args );
    }
}