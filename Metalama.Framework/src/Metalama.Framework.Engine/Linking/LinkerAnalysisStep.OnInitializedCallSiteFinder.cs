// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.RunTime.Initialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System;
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
    // WeakCache.Dispose does nothing, so the class does not need to dispose the cache.
#pragma warning disable CA1001
    private sealed class OnInitializedCallSiteFinder
#pragma warning restore CA1001
    {
        private readonly CompilationContext _compilationContext;
        private readonly CompilationContext _sourceCompilationContext;
        private readonly InitializableTypeRegistry _registry;
        private readonly UserDiagnosticSink _diagnosticSink;
        private readonly IConcurrentTaskRunner _concurrentTaskRunner;
        private readonly INamedTypeSymbol? _initializationContextType;
        private readonly INamedTypeSymbol? _expressionType;
        private readonly WeakCache<ITypeSymbol, bool> _isExpressionTypeCache = new();

        public OnInitializedCallSiteFinder(
            ProjectServiceProvider serviceProvider,
            CompilationContext compilationContext,
            CompilationContext sourceCompilationContext,
            InitializableTypeRegistry registry,
            UserDiagnosticSink diagnosticSink )
        {
            this._compilationContext = compilationContext;
            this._sourceCompilationContext = sourceCompilationContext;
            this._registry = registry;
            this._diagnosticSink = diagnosticSink;
            this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();

            this._initializationContextType =
                compilationContext.Compilation.GetTypeByMetadataName( typeof(InitializationContext).FullName! );

            this._expressionType = compilationContext.Compilation.GetTypeByMetadataName( "System.Linq.Expressions.Expression" );
        }

        /// <summary>
        /// Determines whether a type is <see cref="System.Linq.Expressions.Expression"/> or a type derived from it,
        /// such as <see cref="System.Linq.Expressions.Expression{TDelegate}"/>.
        /// </summary>
        /// <remarks>
        /// The result is cached for each type, because the walkers of all syntax trees evaluate the same conversion types.
        /// </remarks>
        private bool IsExpressionType( ITypeSymbol type ) => this._isExpressionTypeCache.GetOrAdd( type, this.IsExpressionTypeCore );

        /// <summary>
        /// Computes the value returned by <see cref="IsExpressionType"/> when it is not in the cache.
        /// </summary>
        private bool IsExpressionTypeCore( ITypeSymbol type )
        {
            if ( this._expressionType == null )
            {
                return false;
            }

            for ( var t = type; t != null; t = t.BaseType )
            {
                if ( this._compilationContext.SymbolComparer.Equals( t.OriginalDefinition, this._expressionType ) )
                {
                    return true;
                }
            }

            return false;
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
                    var walker = new CallSiteWalker( this, semanticModel, results );

                    walker.Visit( root );
                },
                cancellationToken );

            return results.ToArray();
        }

        /// <summary>
        /// Walks a syntax tree, tracking the containing symbol (method body or field/property
        /// initializer member), and looking for object creation and <c>with</c> expressions
        /// targeting types implementing <c>IInitializable</c>.
        /// </summary>
        private sealed class CallSiteWalker : CSharpSyntaxWalker
        {
            private readonly OnInitializedCallSiteFinder _parent;
            private readonly SemanticModel _semanticModel;
            private readonly InitializableTypeRegistry _registry;
            private readonly INamedTypeSymbol? _initializationContextType;
            private readonly CompilationContext _compilationContext;
            private readonly ConcurrentQueue<ObjectCreationCallSiteReference> _results;
            private ISymbol? _containingSymbol;

            public CallSiteWalker(
                OnInitializedCallSiteFinder parent,
                SemanticModel semanticModel,
                ConcurrentQueue<ObjectCreationCallSiteReference> results )
            {
                this._parent = parent;
                this._semanticModel = semanticModel;
                this._registry = parent._registry;
                this._initializationContextType = parent._initializationContextType;
                this._compilationContext = parent._compilationContext;
                this._results = results;
            }

            public override void VisitMethodDeclaration( MethodDeclarationSyntax node )
            {
                this.VisitWithContainingSymbol( node, this._semanticModel.GetDeclaredSymbol( node ), base.VisitMethodDeclaration );
            }

            public override void VisitConstructorDeclaration( ConstructorDeclarationSyntax node )
            {
                this.VisitWithContainingSymbol( node, this._semanticModel.GetDeclaredSymbol( node ), base.VisitConstructorDeclaration );
            }

            public override void VisitDestructorDeclaration( DestructorDeclarationSyntax node )
            {
                this.VisitWithContainingSymbol( node, this._semanticModel.GetDeclaredSymbol( node ), base.VisitDestructorDeclaration );
            }

            public override void VisitOperatorDeclaration( OperatorDeclarationSyntax node )
            {
                this.VisitWithContainingSymbol( node, this._semanticModel.GetDeclaredSymbol( node ), base.VisitOperatorDeclaration );
            }

            public override void VisitConversionOperatorDeclaration( ConversionOperatorDeclarationSyntax node )
            {
                this.VisitWithContainingSymbol( node, this._semanticModel.GetDeclaredSymbol( node ), base.VisitConversionOperatorDeclaration );
            }

            public override void VisitAccessorDeclaration( AccessorDeclarationSyntax node )
            {
                this.VisitWithContainingSymbol( node, this._semanticModel.GetDeclaredSymbol( node ), base.VisitAccessorDeclaration );
            }

            public override void VisitArrowExpressionClause( ArrowExpressionClauseSyntax node )
            {
                // An expression-bodied property or indexer has no accessor declaration: its expression body is the body
                // of the getter. Attribute the call sites to the getter so that they are rewritten like those of a
                // block-bodied getter. Arrow clauses of methods, accessors and other members are already inside a
                // visited declaration.
                if ( this._containingSymbol == null && this.GetExpressionBodiedGetter( node ) is { } getter )
                {
                    this.VisitWithContainingSymbol( node, getter, base.VisitArrowExpressionClause );
                }
                else
                {
                    base.VisitArrowExpressionClause( node );
                }
            }

            private IMethodSymbol? GetExpressionBodiedGetter( ArrowExpressionClauseSyntax node )
                => node.Parent?.Kind() switch
                {
                    SyntaxKind.PropertyDeclaration when node.Parent is PropertyDeclarationSyntax property =>
                        this._semanticModel.GetDeclaredSymbol( property )?.GetMethod,
                    SyntaxKind.IndexerDeclaration when node.Parent is IndexerDeclarationSyntax indexer =>
                        this._semanticModel.GetDeclaredSymbol( indexer )?.GetMethod,
                    _ => null
                };

            public override void VisitLocalFunctionStatement( LocalFunctionStatementSyntax node )
            {
                var localFunctionSymbol = this._semanticModel.GetDeclaredSymbol( node );
                this.VisitWithContainingSymbol( node, localFunctionSymbol, base.VisitLocalFunctionStatement );
            }

            public override void VisitEqualsValueClause( EqualsValueClauseSyntax node )
            {
                // Attribute object-creation expressions in field/property/event initializers to the
                // declaring member, so the rewrite pipeline wraps them with WithInitialize(...).
                // Only applies when we're not already inside a method body (in which case local
                // variable initializers correctly inherit the enclosing method symbol).
                if ( this._containingSymbol == null && this.GetInitializerMemberSymbol( node ) is { } memberSymbol )
                {
                    this.VisitWithContainingSymbol( node, memberSymbol, base.VisitEqualsValueClause );
                }
                else
                {
                    base.VisitEqualsValueClause( node );
                }
            }

            private ISymbol? GetInitializerMemberSymbol( EqualsValueClauseSyntax node )
            {
                switch ( node.Parent?.Kind() )
                {
                    case SyntaxKind.VariableDeclarator when node.Parent is VariableDeclaratorSyntax declarator:
                        var grandparentKind = declarator.Parent?.Parent?.Kind();

                        if ( grandparentKind is SyntaxKind.FieldDeclaration or SyntaxKind.EventFieldDeclaration )
                        {
                            return this._semanticModel.GetDeclaredSymbol( declarator );
                        }

                        return null;

                    case SyntaxKind.PropertyDeclaration when node.Parent is PropertyDeclarationSyntax propertyDeclaration:
                        return this._semanticModel.GetDeclaredSymbol( propertyDeclaration );

                    default:
                        return null;
                }
            }

            // The caller must resolve the declared symbol itself — it cannot be derived here from the node.
            // Calling SemanticModel.GetDeclaredSymbol( node ) with a generic T : SyntaxNode binds to the base
            // SyntaxNode overload that always returns null; only the typed CSharpExtensions overloads
            // (e.g. GetDeclaredSymbol( MethodDeclarationSyntax )) return a useful symbol. Each VisitXxx
            // override therefore resolves the symbol at its own concrete type before delegating here.
            private void VisitWithContainingSymbol<T>( T node, ISymbol? symbol, Action<T> baseVisit )
                where T : SyntaxNode
            {
                if ( symbol == null )
                {
                    return;
                }

                var previousContaining = this._containingSymbol;
                this._containingSymbol = symbol;

                baseVisit( node );

                this._containingSymbol = previousContaining;
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

                if ( this._containingSymbol == null )
                {
                    return;
                }

                var typeInfo = this._semanticModel.GetTypeInfo( node );

                if ( typeInfo.Type is { Kind: SymbolKind.NamedType } and INamedTypeSymbol namedType
                     && this._registry.TryGetTypeInfo( namedType, out var onInitInfo ) )
                {
                    this._results.Enqueue(
                        new ObjectCreationCallSiteReference(
                            this._containingSymbol,
                            node,
                            isWithExpression: true,
                            onInitInfo,
                            constructor: null,
                            contextParamName: null ) );
                }
            }

            private void ProcessObjectCreation( ExpressionSyntax node, ArgumentListSyntax? argumentList )
            {
                if ( this._containingSymbol == null )
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

                // An expression tree cannot contain the call to WithInitialize, so the call site is left unchanged (#2273).
                if ( this.IsInExpressionTree( node ) )
                {
                    this.ReportInitializeNotCalledInExpressionTree( node, constructedType );

                    return;
                }

                var contextParamName = onInitInfo.GetContextParamName( constructor );

                this._results.Enqueue(
                    new ObjectCreationCallSiteReference(
                        this._containingSymbol,
                        node,
                        isWithExpression: false,
                        onInitInfo,
                        constructor,
                        contextParamName ) );
            }

            /// <summary>
            /// Determines whether an expression is in a lambda expression or an anonymous method that is converted to an expression tree.
            /// </summary>
            /// <remarks>
            /// The method also covers the lambda expressions that the compiler creates for the clauses of a query expression.
            /// </remarks>
            private bool IsInExpressionTree( ExpressionSyntax node )
            {
                if ( !node.Ancestors().Any( a => a is AnonymousFunctionExpressionSyntax or QueryExpressionSyntax ) )
                {
                    return false;
                }

                for ( var operation = this._semanticModel.GetOperation( node ); operation != null; operation = operation.Parent )
                {
                    if ( operation is IAnonymousFunctionOperation { Parent: IConversionOperation { Type: { } conversionType } }
                         && this._parent.IsExpressionType( conversionType ) )
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Reports a warning that the <c>Initialize</c> method is not called after an object creation expression in an expression tree.
            /// </summary>
            private void ReportInitializeNotCalledInExpressionTree( ExpressionSyntax node, INamedTypeSymbol constructedType )
            {
                var location = this.GetSourceLocation( node );

                if ( location == null )
                {
                    return;
                }

                this._parent._diagnosticSink.Report(
                    AspectLinkerDiagnosticDescriptors.InitializeNotCalledInExpressionTree.CreateRoslynDiagnostic( location, constructedType ) );
            }

            /// <summary>
            /// Gets the location, in the source compilation, of an expression of the intermediate compilation.
            /// </summary>
            /// <remarks>
            /// The intermediate syntax tree differs from the source syntax tree. The method finds the declaration of the
            /// containing symbol in the source compilation, and searches the expression at the same offset in this declaration.
            /// When the expression is not found, the method returns the location of the containing symbol in the source compilation.
            /// </remarks>
            private Location? GetSourceLocation( ExpressionSyntax node )
            {
                for ( var symbol = this._containingSymbol; symbol != null; symbol = symbol.ContainingSymbol )
                {
                    // A local function cannot be translated to the source compilation, so the expression is searched in the enclosing member.
                    if ( symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction } )
                    {
                        continue;
                    }

                    var sourceSymbol = this._parent._sourceCompilationContext.SymbolTranslator.Translate( symbol, allowMultiple: true );

                    if ( sourceSymbol == null )
                    {
                        continue;
                    }

                    var intermediateDeclaration = symbol.DeclaringSyntaxReferences
                        .Select( r => r.GetSyntax() )
                        .FirstOrDefault( s => s.SyntaxTree == node.SyntaxTree && s.Span.Contains( node.Span ) );

                    if ( intermediateDeclaration != null )
                    {
                        var offset = node.SpanStart - intermediateDeclaration.SpanStart;

                        foreach ( var sourceReference in sourceSymbol.DeclaringSyntaxReferences )
                        {
                            var sourceDeclaration = sourceReference.GetSyntax();
                            var sourceSpan = new TextSpan( sourceDeclaration.SpanStart + offset, node.Span.Length );

                            if ( sourceDeclaration.Span.Contains( sourceSpan )
                                 && sourceDeclaration.FindNode( sourceSpan, getInnermostNodeForTie: true ) is { } sourceNode
                                 && sourceNode.IsEquivalentTo( node ) )
                            {
                                return sourceNode.GetLocation();
                            }
                        }
                    }

                    return sourceSymbol.GetDiagnosticLocation();
                }

                return null;
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