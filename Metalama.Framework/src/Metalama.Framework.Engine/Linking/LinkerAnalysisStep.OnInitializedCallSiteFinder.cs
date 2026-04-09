// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.RunTime.Initialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    /// <summary>
    /// Walks syntax trees in the intermediate compilation looking for object creation expressions
    /// and <c>with</c> expressions that target types implementing <c>IInitializable</c>.
    /// </summary>
    private sealed class OnInitializedCallSiteFinder
    {
        private readonly CompilationContext _compilationContext;
        private readonly InitializableTypeRegistry _registry;
        private readonly IConcurrentTaskRunner _concurrentTaskRunner;
        private readonly INamedTypeSymbol? _initializationContextType;

        public OnInitializedCallSiteFinder(
            ProjectServiceProvider serviceProvider,
            CompilationContext compilationContext,
            InitializableTypeRegistry registry )
        {
            this._compilationContext = compilationContext;
            this._registry = registry;
            this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();

            this._initializationContextType =
                compilationContext.Compilation.GetTypeByMetadataName( typeof(InitializationContext).FullName! );
        }

        public async Task<IReadOnlyList<ObjectCreationCallSiteReference>> FindCallSitesAsync( CancellationToken cancellationToken )
        {
            var results = new ConcurrentQueue<ObjectCreationCallSiteReference>();
            var syntaxTrees = this._compilationContext.Compilation.SyntaxTrees.ToList();

            await this._concurrentTaskRunner.RunConcurrentlyAsync(
                syntaxTrees,
                syntaxTree =>
                {
                    var semanticModel = this._compilationContext.SemanticModelProvider.GetSemanticModel( syntaxTree );
                    var root = syntaxTree.GetRoot( cancellationToken );
                    var walker = new CallSiteWalker( semanticModel, this._registry, this._initializationContextType, this._compilationContext, results );

                    walker.Visit( root );
                },
                cancellationToken );

            return results.ToArray();
        }

        /// <summary>
        /// Walks a syntax tree, tracking the containing method symbol, and looking for
        /// object creation and <c>with</c> expressions targeting types implementing <c>IInitializable</c>.
        /// </summary>
        private sealed class CallSiteWalker : CSharpSyntaxWalker
        {
            private readonly SemanticModel _semanticModel;
            private readonly InitializableTypeRegistry _registry;
            private readonly INamedTypeSymbol? _initializationContextType;
            private readonly CompilationContext _compilationContext;
            private readonly ConcurrentQueue<ObjectCreationCallSiteReference> _results;
            private IMethodSymbol? _containingMethod;

            public CallSiteWalker(
                SemanticModel semanticModel,
                InitializableTypeRegistry registry,
                INamedTypeSymbol? initializationContextType,
                CompilationContext compilationContext,
                ConcurrentQueue<ObjectCreationCallSiteReference> results )
            {
                this._semanticModel = semanticModel;
                this._registry = registry;
                this._initializationContextType = initializationContextType;
                this._compilationContext = compilationContext;
                this._results = results;
            }

            public override void VisitMethodDeclaration( MethodDeclarationSyntax node )
            {
                this.VisitWithContainingMethod( node, this._semanticModel.GetDeclaredSymbol( node ), base.VisitMethodDeclaration );
            }

            public override void VisitConstructorDeclaration( ConstructorDeclarationSyntax node )
            {
                this.VisitWithContainingMethod( node, this._semanticModel.GetDeclaredSymbol( node ), base.VisitConstructorDeclaration );
            }

            public override void VisitDestructorDeclaration( DestructorDeclarationSyntax node )
            {
                this.VisitWithContainingMethod( node, this._semanticModel.GetDeclaredSymbol( node ), base.VisitDestructorDeclaration );
            }

            public override void VisitOperatorDeclaration( OperatorDeclarationSyntax node )
            {
                this.VisitWithContainingMethod( node, this._semanticModel.GetDeclaredSymbol( node ), base.VisitOperatorDeclaration );
            }

            public override void VisitConversionOperatorDeclaration( ConversionOperatorDeclarationSyntax node )
            {
                this.VisitWithContainingMethod( node, this._semanticModel.GetDeclaredSymbol( node ), base.VisitConversionOperatorDeclaration );
            }

            public override void VisitAccessorDeclaration( AccessorDeclarationSyntax node )
            {
                this.VisitWithContainingMethod( node, this._semanticModel.GetDeclaredSymbol( node ), base.VisitAccessorDeclaration );
            }

            public override void VisitLocalFunctionStatement( LocalFunctionStatementSyntax node )
            {
                this.VisitWithContainingMethod( node, this._semanticModel.GetDeclaredSymbol( node ), base.VisitLocalFunctionStatement );
            }

            // The caller must resolve the method symbol itself — it cannot be derived here from the node.
            // Calling SemanticModel.GetDeclaredSymbol( node ) with a generic T : SyntaxNode binds to the base
            // SyntaxNode overload that always returns null; only the typed CSharpExtensions overloads
            // (e.g. GetDeclaredSymbol( MethodDeclarationSyntax )) return an IMethodSymbol. Each VisitXxx
            // override therefore resolves the symbol at its own concrete type before delegating here.
            private void VisitWithContainingMethod<T>( T node, IMethodSymbol? methodSymbol, System.Action<T> baseVisit )
                where T : SyntaxNode
            {
                if ( methodSymbol == null )
                {
                    return;
                }

                var previousContaining = this._containingMethod;
                this._containingMethod = methodSymbol;

                baseVisit( node );

                this._containingMethod = previousContaining;
            }

            public override void VisitObjectCreationExpression( ObjectCreationExpressionSyntax node )
            {
                base.VisitObjectCreationExpression( node );
                this.ProcessObjectCreation( node, node.ArgumentList );
            }

            public override void VisitImplicitObjectCreationExpression( ImplicitObjectCreationExpressionSyntax node )
            {
                base.VisitImplicitObjectCreationExpression( node );
                this.ProcessObjectCreation( node, node.ArgumentList );
            }

            public override void VisitWithExpression( WithExpressionSyntax node )
            {
                base.VisitWithExpression( node );

                if ( this._containingMethod == null )
                {
                    return;
                }

                var typeInfo = this._semanticModel.GetTypeInfo( node );

                if ( typeInfo.Type is { Kind: SymbolKind.NamedType } and INamedTypeSymbol namedType
                     && this._registry.TryGetTypeInfo( namedType, out var onInitInfo ) )
                {
                    this._results.Enqueue(
                        new ObjectCreationCallSiteReference(
                            this._containingMethod.ToSemantic( IntermediateSymbolSemanticKind.Default ),
                            node,
                            isWithExpression: true,
                            onInitInfo,
                            constructor: null,
                            contextParamName: null ) );
                }
            }

            private void ProcessObjectCreation( ExpressionSyntax node, ArgumentListSyntax? argumentList )
            {
                if ( this._containingMethod == null )
                {
                    return;
                }

                var symbolInfo = this._semanticModel.GetSymbolInfo( node );

                if ( symbolInfo.Symbol is not IMethodSymbol constructor )
                {
                    return;
                }

                var constructedType = constructor.ContainingType;

                if ( !this._registry.TryGetTypeInfo( constructedType, out var onInitInfo ) )
                {
                    return;
                }

                // Skip if user already passes InitializationContext argument.
                if ( argumentList != null && this.HasInitializationContextArgument( argumentList ) )
                {
                    return;
                }

                var contextParamName = onInitInfo.GetContextParamName( constructor );

                this._results.Enqueue(
                    new ObjectCreationCallSiteReference(
                        this._containingMethod.ToSemantic( IntermediateSymbolSemanticKind.Default ),
                        node,
                        isWithExpression: false,
                        onInitInfo,
                        constructor,
                        contextParamName ) );
            }

            private bool HasInitializationContextArgument( ArgumentListSyntax argumentList )
            {
                if ( this._initializationContextType == null )
                {
                    return false;
                }

                foreach ( var argument in argumentList.Arguments )
                {
                    var argTypeInfo = this._semanticModel.GetTypeInfo( argument.Expression );

                    if ( argTypeInfo.Type != null
                         && this._compilationContext.SymbolComparer.Equals( argTypeInfo.Type, this._initializationContextType ) )
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
