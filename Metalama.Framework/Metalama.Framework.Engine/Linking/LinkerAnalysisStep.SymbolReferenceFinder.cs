// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    private sealed class SymbolReferenceFinder
    {
        // PERF - Use cases:
        //   1) GetCallerAttributeReferencesAsync:
        //          get all references from a overridden method source to any other method - caller attributes have to be fixed on call site.
        //          This is optimized by indexing all invocation expressions.
        //   2) GetRedirectedGetOnlyAutoPropertyReferencesAsync:
        //          get all references to get-only auto property setters that were overridden - setter reference has to be changed to backing storage reference.
        //   3) GetEventFieldRaiseReferencesAsync:
        //          get all reference to overridden event field raise (e.g. invocation expression, Invoke, BeginInvoke, etc.) - these expressions should reference the backing event field instead.

        private readonly CompilationContext _compilationContext;
        private readonly IConcurrentTaskRunner _concurrentTaskRunner;
        private readonly ConcurrentDictionary<IMethodSymbol, MethodCacheRecord> _methodCache;

        public SymbolReferenceFinder(
            ProjectServiceProvider serviceProvider,
            CompilationContext compilationContext )
        {
            this._compilationContext = compilationContext;
            this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
            this._methodCache = new ConcurrentDictionary<IMethodSymbol, MethodCacheRecord>();
        }

        internal async Task<IEnumerable<IntermediateSymbolSemanticReference>> FindSymbolReferencesAsync<TSymbol, TSource>(
            IReadOnlyList<TSource> sources,
            Func<TSource, (TSymbol ReferencedSymbol, INamedTypeSymbol ReferencingType)> getSourceRecord,
            Func<ISymbol, bool> referencingSymbolPredicate,
            CancellationToken cancellationToken )
            where TSymbol : ISymbol
        {
            var referencingTypes = new Dictionary<INamedTypeSymbol, HashSet<ISymbol>>( this._compilationContext.SymbolComparer );

            foreach ( var source in sources )
            {
                var sourceRecord = getSourceRecord( source );

                if ( !referencingTypes.TryGetValue( sourceRecord.ReferencingType, out var referencedSymbol ) )
                {
                    referencingTypes[sourceRecord.ReferencingType] =
                        referencedSymbol = new HashSet<ISymbol>( this._compilationContext.SymbolComparer );
                }

                referencedSymbol.Add( sourceRecord.ReferencedSymbol );
            }

            var methodsToAnalyze = new List<(IMethodSymbol Method, HashSet<ISymbol> SymbolsToFind)>();
            var symbolReferences = new ConcurrentQueue<IntermediateSymbolSemanticReference>();

            foreach ( var referencingType in referencingTypes )
            {
                // Only take methods.
                foreach ( var member in referencingType.Key.GetMembers() )
                {
                    switch ( member )
                    {
                        case IMethodSymbol method:
                            if ( referencingSymbolPredicate( method ) )
                            {
                                methodsToAnalyze.Add( (method, referencingType.Value) );
                            }

                            break;
                    }
                }
            }

            void ProcessMethod( (IMethodSymbol Method, HashSet<ISymbol> SymbolsToFind) input )
            {
                foreach ( var symbolToFind in input.SymbolsToFind )
                {
                    foreach ( var referencingIdentifierName in this.GetReferencingIdentifierNames( input.Method, symbolToFind ) )
                    {
                        symbolReferences.Enqueue(
                            new IntermediateSymbolSemanticReference(
                                input.Method.ToSemantic( IntermediateSymbolSemanticKind.Default ),
                                symbolToFind.ToSemantic( IntermediateSymbolSemanticKind.Default ),
                                referencingIdentifierName ) );
                    }
                }
            }

            await this._concurrentTaskRunner.RunConcurrentlyAsync( methodsToAnalyze, ProcessMethod, cancellationToken );

            return symbolReferences.ToReadOnlyList();
        }

        internal async Task<IEnumerable<IntermediateSymbolSemanticReference>> FindMethodInvocationsAsync(
            IEnumerable<IMethodSymbol> methodsToAnalyze,
            CancellationToken cancellationToken )
        {
            var symbolReferences = new ConcurrentQueue<IntermediateSymbolSemanticReference>();

            void ProcessMethod( IMethodSymbol method )
            {
                var invocationExpressionRecords = this.GetContainedInvocations( method );
                SyntaxTree? currentSyntaxTree = null;
                SemanticModel? currentSemanticModel = null;

                foreach ( var invocationExpressionRecord in invocationExpressionRecords )
                {
                    if ( invocationExpressionRecord.Node.SyntaxTree != currentSyntaxTree )
                    {
                        currentSemanticModel = this._compilationContext.SemanticModelProvider.GetSemanticModel( invocationExpressionRecord.Node.SyntaxTree );
                    }

                    var symbol = invocationExpressionRecord.GetSymbol( currentSemanticModel! );

                    if ( symbol == null )
                    {
                        continue;
                    }

                    symbolReferences.Enqueue(
                        new IntermediateSymbolSemanticReference(
                            method.ToSemantic( IntermediateSymbolSemanticKind.Default ),
                            symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ),
                            invocationExpressionRecord.Node ) );
                }
            }

            await this._concurrentTaskRunner.RunConcurrentlyAsync( methodsToAnalyze, ProcessMethod, cancellationToken );

            return symbolReferences.ToReadOnlyList();
        }

        private IEnumerable<SyntaxSymbolCacheRecord<InvocationExpressionSyntax>> GetContainedInvocations( IMethodSymbol containingMethod )
        {
            var methodRecord = this._methodCache.GetOrAdd( containingMethod, AnalyzeMethod );

            return methodRecord.InvocationExpressions;
        }

        private IEnumerable<IdentifierNameSyntax> GetReferencingIdentifierNames( IMethodSymbol containingMethod, ISymbol targetSymbol )
        {
            var methodRecord = this._methodCache.GetOrAdd( containingMethod, AnalyzeMethod );

            if ( methodRecord.IdentifierLookup.TryGetValue( targetSymbol.Name, out var identifierList ) )
            {
                // The semantic model is unlikely to change and therefore we can cache it in a variable.
                SyntaxTree? currentSyntaxTree = null;
                SemanticModel? currentSemanticModel = null;
                var comparer = this._compilationContext.SymbolComparer;

                foreach ( var identifierRecord in identifierList )
                {
                    if ( identifierRecord.Node.SyntaxTree != currentSyntaxTree )
                    {
                        currentSemanticModel = this._compilationContext.SemanticModelProvider.GetSemanticModel( identifierRecord.Node.SyntaxTree );
                    }

                    var foundSymbol = identifierRecord.GetSymbol( currentSemanticModel! );

                    if ( comparer.Equals( targetSymbol, foundSymbol.GetCanonicalDefinition() ) )
                    {
                        yield return identifierRecord.Node;
                    }
                }
            }
        }

        private static MethodCacheRecord AnalyzeMethod( IMethodSymbol method )
        {
            var walker = new BodyWalker();

            foreach ( var declaration in method.DeclaringSyntaxReferences )
            {
                walker.Visit( declaration.GetSyntax() );
            }

            return new MethodCacheRecord( method, walker.IdentifierSyntaxSymbolLookup, walker.InvocationExpressionSyntaxSymbolLookup );
        }

        /// <summary>
        /// Finds IdentifierNameSyntax and initializes per-method cache.
        /// </summary>
        private sealed class BodyWalker : CSharpSyntaxWalker
        {
            // TODO: Both caches should use unified symbols.
            public Dictionary<string, List<SyntaxSymbolCacheRecord<IdentifierNameSyntax>>> IdentifierSyntaxSymbolLookup { get; }

            public List<SyntaxSymbolCacheRecord<InvocationExpressionSyntax>> InvocationExpressionSyntaxSymbolLookup { get; }

            public BodyWalker()
            {
                this.IdentifierSyntaxSymbolLookup = new Dictionary<string, List<SyntaxSymbolCacheRecord<IdentifierNameSyntax>>>();
                this.InvocationExpressionSyntaxSymbolLookup = new List<SyntaxSymbolCacheRecord<InvocationExpressionSyntax>>();
            }

            public override void VisitIdentifierName( IdentifierNameSyntax node )
            {
                var text = node.Identifier.ValueText;

                this.IdentifierSyntaxSymbolLookup
                    .GetOrAdd( text, _ => new List<SyntaxSymbolCacheRecord<IdentifierNameSyntax>>() )
                    .Add( new SyntaxSymbolCacheRecord<IdentifierNameSyntax>( node ) );

                base.VisitIdentifierName( node );
            }

            public override void VisitInvocationExpression( InvocationExpressionSyntax node )
            {
                if ( node is not { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" }, ArgumentList.Arguments.Count: 1 } )
                {
                    // If there is a method named "nameof" with 1 argument, we may skip it too, but it's a very narrow edge-case.
                    this.InvocationExpressionSyntaxSymbolLookup.Add( new SyntaxSymbolCacheRecord<InvocationExpressionSyntax>( node ) );
                }

                base.VisitInvocationExpression( node );
            }
        }

        // ReSharper disable once NotAccessedPositionalProperty.Local
        private record struct MethodCacheRecord(
            IMethodSymbol Method,
            Dictionary<string, List<SyntaxSymbolCacheRecord<IdentifierNameSyntax>>> IdentifierLookup,
            List<SyntaxSymbolCacheRecord<InvocationExpressionSyntax>> InvocationExpressions );

        private sealed class SyntaxSymbolCacheRecord<TSyntax>
            where TSyntax : SyntaxNode
        {
            private volatile bool _isInitialized;
            private volatile ISymbol? _cachedSymbol;

            public TSyntax Node { get; }

            public SyntaxSymbolCacheRecord( TSyntax name )
            {
                this.Node = name;
            }

            public ISymbol? GetSymbol( SemanticModel semanticModel )
            {
                if ( !this._isInitialized )
                {
                    var symbolInfo = semanticModel.GetSymbolInfo( this.Node );

                    // Lock after getting the symbol to lower the probability of blocking the thread.
                    lock ( this )
                    {
                        if ( !this._isInitialized )
                        {
                            this._cachedSymbol = symbolInfo.Symbol;
                            this._isInitialized = true;

                            return this._cachedSymbol;
                        }
                    }
                }

                return this._cachedSymbol;
            }
        }
    }
}