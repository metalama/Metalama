// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    /// <summary>
    /// Walks syntax trees, looking for and resolving aspect references.
    /// </summary>
    private sealed class AspectReferenceWalker : SafeSyntaxWalker
    {
        private readonly AspectReferenceResolver _referenceResolver;
        private readonly SemanticModel _semanticModel;
        private readonly IMethodSymbol _containingSymbol;
        private readonly Stack<IMethodSymbol> _localFunctionStack;
        private readonly HashSet<SyntaxNode> _nodesContainingAspectReferences;

        public List<ResolvedAspectReference> AspectReferences { get; }

        public AspectReferenceWalker(
            AspectReferenceResolver referenceResolver,
            SemanticModel semanticModel,
            IMethodSymbol containingSymbol,
            HashSet<SyntaxNode> nodesContainingAspectReferences )
        {
            this._referenceResolver = referenceResolver;
            this._semanticModel = semanticModel;
            this.AspectReferences = new List<ResolvedAspectReference>();
            this._containingSymbol = containingSymbol;
            this._localFunctionStack = new Stack<IMethodSymbol>();
            this._nodesContainingAspectReferences = nodesContainingAspectReferences;
        }

        public override void VisitLocalFunctionStatement( LocalFunctionStatementSyntax node )
        {
            var symbol = (IMethodSymbol) this._semanticModel.GetDeclaredSymbol( node ).AssertNotNull();

            try
            {
                this._localFunctionStack.Push( symbol );

                base.VisitLocalFunctionStatement( node );
            }
            finally
            {
                this._localFunctionStack.Pop();
            }
        }

        protected override void VisitCore( SyntaxNode? node )
        {
            if ( node == null )
            {
                // Coverage: ignore (irrelevant).
                return;
            }

            if ( node.TryGetAspectReference( out var aspectReference ) )
            {
                var nodeWithSymbol = node switch
                {
                    ConditionalAccessExpressionSyntax conditionalAccess => GetConditionalMemberName( conditionalAccess ),
                    _ => node
                };

                var symbolInfo = this._semanticModel.GetSymbolInfo( nodeWithSymbol );

                var referencedSymbol =
                    symbolInfo switch
                    {
                        // Normal situation with valid symbol.
                        { Symbol: { } symbol } => symbol,

                        // In most invalid code situations, there is one candidate symbol.
                        { CandidateSymbols: [{ } symbol] } => symbol,

                        // Otherwise we will skip this reference completely, which will cause it not to be transformed.
                        _ => null,
                    };

                if ( referencedSymbol == null )
                {
                    // Return if we could not resolve the symbol.
                    return;
                }

                IMethodSymbol? localFunction = null;

                if ( this._localFunctionStack.Count > 0 )
                {
                    localFunction = this._localFunctionStack.Peek();
                }

                var resolvedReference = this._referenceResolver.Resolve(
                    this._containingSymbol.ToSemantic( IntermediateSymbolSemanticKind.Default ),
                    localFunction,
                    referencedSymbol,
                    (ExpressionSyntax) node,
                    aspectReference,
                    this._semanticModel );

                this.AspectReferences.Add( resolvedReference );
            }

            if ( this._nodesContainingAspectReferences.Contains( node ) )
            {
                // Visit only when an aspect reference exists in the subtree of the node.
                base.VisitCore( node );
            }

            static MemberBindingExpressionSyntax GetConditionalMemberName( ConditionalAccessExpressionSyntax conditionalAccess )
            {
                var walker = new ConditionalAccessExpressionWalker();

                walker.Visit( conditionalAccess );

                return walker.FoundName.AssertNotNull();
            }
        }

        private sealed class ConditionalAccessExpressionWalker : SafeSyntaxWalker
        {
            private ConditionalAccessExpressionSyntax? _context;

            public MemberBindingExpressionSyntax? FoundName { get; private set; }

            public override void VisitConditionalAccessExpression( ConditionalAccessExpressionSyntax node )
            {
                if ( this._context == null )
                {
                    this._context = node;

                    this.Visit( node.WhenNotNull );

                    this._context = null;
                }
            }

            public override void VisitMemberBindingExpression( MemberBindingExpressionSyntax node )
            {
                if ( this._context != null )
                {
                    this.FoundName = node;
                }
            }
        }
    }
}