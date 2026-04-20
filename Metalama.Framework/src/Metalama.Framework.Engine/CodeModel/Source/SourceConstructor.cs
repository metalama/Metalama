// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using ConstructorInvoker = Metalama.Framework.Engine.CodeModel.Invokers.ConstructorInvoker;
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
            Invariant.Assert(
                symbol.PartialDefinitionPart == null,
                "Cannot use partial implementation to instantiate the SourceConstructor class." );

            if ( symbol.MethodKind != RoslynMethodKind.Constructor && symbol.MethodKind != RoslynMethodKind.StaticConstructor )
            {
                throw new ArgumentOutOfRangeException( nameof(symbol), "The Constructor class must be used only with constructors." );
            }
        }

        [Memo]
        public override ImmutableArray<SourceReference> Sources => this.GetSourcesImpl();

        private ImmutableArray<SourceReference> GetSourcesImpl()
        {
            if ( this.MethodSymbol.PartialImplementationPart != null )
            {
                return
                    ImmutableArray.Create(
                        new SourceReference( this.MethodSymbol.DeclaringSyntaxReferences[0].GetSyntax(), SourceReferenceImpl.Instance ),
                        new SourceReference(
                            this.MethodSymbol.PartialImplementationPart.DeclaringSyntaxReferences[0].GetSyntax(),
                            SourceReferenceImpl.Instance ) );
            }
            else
            {
                return base.Sources;
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
            => this.MethodSymbol.MethodKind == RoslynMethodKind.StaticConstructor
                ? ConstructorInitializerKind.None
                : this.GetPrimaryDeclarationSyntax()?.Kind() switch
                {
                    null => this.MethodSymbol.ContainingType.IsValueType ? ConstructorInitializerKind.None : ConstructorInitializerKind.Base,
                    SyntaxKind.ConstructorDeclaration when this.GetPrimaryDeclarationSyntax() is ConstructorDeclarationSyntax { Initializer: null } =>
                        this.MethodSymbol.ContainingType.IsValueType ? ConstructorInitializerKind.None : ConstructorInitializerKind.Base,
                    SyntaxKind.ConstructorDeclaration when this.GetPrimaryDeclarationSyntax() is ConstructorDeclarationSyntax { Initializer: { } initializer }
                                                           && initializer.IsKind( SyntaxKind.ThisConstructorInitializer ) =>
                        ConstructorInitializerKind.This,
                    SyntaxKind.ConstructorDeclaration when this.GetPrimaryDeclarationSyntax() is ConstructorDeclarationSyntax { Initializer: { } initializer }
                                                           && initializer.IsKind( SyntaxKind.BaseConstructorInitializer ) =>
                        ConstructorInitializerKind.Base,
                    { IsTypeDeclaration: true } when this.GetPrimaryDeclarationSyntax() is TypeDeclarationSyntax { BaseList: null } =>
                        this.MethodSymbol.ContainingType.IsValueType ? ConstructorInitializerKind.None : ConstructorInitializerKind.Base,
                    { IsTypeDeclaration: true } when this.GetPrimaryDeclarationSyntax() is TypeDeclarationSyntax { BaseList: { } baseList } =>
                        baseList.Types.Any( bt => bt.IsKind( SyntaxKind.PrimaryConstructorBaseType ) )
                            ? ConstructorInitializerKind.Base
                            : this.MethodSymbol.ContainingType.IsValueType
                                ? ConstructorInitializerKind.None
                                : ConstructorInitializerKind.Base,
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

            SyntaxNode? initializer = declaration?.Kind() switch
            {
                null => null,
                SyntaxKind.ConstructorDeclaration when declaration is ConstructorDeclarationSyntax constructorDeclaration => constructorDeclaration.Initializer,
                SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration
                    when declaration is TypeDeclarationSyntax typeDeclarationSyntax =>
                    typeDeclarationSyntax.BaseList?.Types.FirstOrDefault() as PrimaryConstructorBaseTypeSyntax,
                _ => throw new AssertionFailedException( $"Unexpected constructor syntax {declaration.GetType()}." )
            };

            if ( initializer == null )
            {
                return BaseConstructorResolver.GetImplicitBaseConstructor( this.DeclaringType );
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

        [Memo]
        private IConstructorInvoker Invoker => new ConstructorInvoker( this );

        object? IConstructorInvoker.Invoke( params object?[] args ) => this.Invoker.Invoke( args );

        object? IConstructorInvoker.Invoke( IEnumerable<IExpression> args ) => this.Invoker.Invoke( args );

        IObjectCreationExpression IConstructorInvoker.CreateInvokeExpression() => this.Invoker.CreateInvokeExpression();

        IObjectCreationExpression IConstructorInvoker.CreateInvokeExpression( params object?[] args ) => this.Invoker.CreateInvokeExpression( args );

        IObjectCreationExpression IConstructorInvoker.CreateInvokeExpression( IEnumerable<IExpression> args ) => this.Invoker.CreateInvokeExpression( args );
    }
}