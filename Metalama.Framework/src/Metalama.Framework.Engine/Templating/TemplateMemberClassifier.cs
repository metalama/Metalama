// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.CompileTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Engine.Templating
{
    /// <summary>
    /// Provides methods that tests for classifications of template members.
    /// </summary>
    internal sealed class TemplateMemberClassifier : TemplateMemberSymbolClassifier
    {
        private readonly SyntaxTreeAnnotationMap _syntaxTreeAnnotationMap;

        public TemplateMemberClassifier(
            ClassifyingCompilationContext runTimeCompilationContext,
            SyntaxTreeAnnotationMap syntaxTreeAnnotationMap ) : base( runTimeCompilationContext.SymbolClassifier )
        {
            var reflectionMapper = runTimeCompilationContext.ReflectionMapper;
            this.MetaType = reflectionMapper.GetTypeSymbol( typeof(meta) );
            this._syntaxTreeAnnotationMap = syntaxTreeAnnotationMap;
        }

        private ITypeSymbol MetaType { get; }

        public bool IsRunTimeMethod( IMethodSymbol symbol )
            => symbol.Name == nameof(meta.RunTime) &&
               symbol.ContainingType.GetDocumentationCommentId() == this.MetaType.GetDocumentationCommentId();

        public bool IsTemplateParameter( ExpressionSyntax expression )
        {
            var symbol = this._syntaxTreeAnnotationMap.GetSymbol( expression );

            if ( symbol is IParameterSymbol parameter )
            {
                return IsTemplateParameter( parameter );
            }
            else
            {
                return false;
            }
        }

        public bool IsRunTimeMethod( SyntaxNode node )
            => this._syntaxTreeAnnotationMap.GetSymbol( node ) is IMethodSymbol symbol && this.IsRunTimeMethod( symbol );

        public bool IsNodeOfDynamicType( SyntaxNode originalNode ) => originalNode is ExpressionSyntax expression && this.IsNodeOfDynamicType( expression );

        /// <summary>
        /// Determines if a node is of <c>dynamic</c> type.
        /// </summary>
        /// <param name="originalNode"></param>
        /// <returns></returns>
        public bool IsNodeOfDynamicType( ExpressionSyntax originalNode )
        {
            var expressionType = this._syntaxTreeAnnotationMap.GetExpressionType( originalNode );

            if ( expressionType is IDynamicTypeSymbol )
            {
                // Roslyn returns a dynamic type even for methods returning a non-dynamic type, as long as they have at least
                // one dynamic argument. We don't want to fix the Roslyn type resolution, but in the specific case of void methods,
                // we can do it without a chance of being ever wrong. It allows meta.DefineExpression to work.
                if ( originalNode is InvocationExpressionSyntax )
                {
                    var symbols = this._syntaxTreeAnnotationMap.GetCandidateSymbols( originalNode ).ToReadOnlyList();

                    if ( symbols.Count > 0 && symbols.All( symbol => symbol is IMethodSymbol { ReturnsVoid: true } ) )
                    {
                        return false;
                    }
                }
            }

            if ( expressionType != null && this.SymbolClassifier.GetTemplatingScope( expressionType ) == TemplatingScope.Dynamic )
            {
                return true;
            }

            var nodeSymbol = this._syntaxTreeAnnotationMap.GetSymbol( originalNode );

            return (nodeSymbol is IMethodSymbol method && this.SymbolClassifier.GetTemplatingScope( method.ReturnType ) == TemplatingScope.Dynamic) ||
                   (nodeSymbol is IPropertySymbol property && this.SymbolClassifier.GetTemplatingScope( property.Type ) == TemplatingScope.Dynamic);
        }

        /// <summary>
        /// Determines if the node is a pragma and returns the kind of pragma, if any.
        /// </summary>
        public MetaMemberKind GetMetaMemberKind( SyntaxNode node )
        {
            var symbol = this._syntaxTreeAnnotationMap.GetSymbol( node );

            return this.GetMetaMemberKind( symbol );
        }

        private MetaMemberKind GetMetaMemberKind( ISymbol? symbol )
        {
            if ( symbol == null )
            {
                return MetaMemberKind.None;
            }
            else if ( SymbolEqualityComparer.Default.Equals( symbol.ContainingType, this.MetaType ) )
            {
                return symbol.Name switch
                {
                    nameof(meta.This) => MetaMemberKind.This,
                    nameof(meta.InsertComment) => MetaMemberKind.InsertComment,
                    nameof(meta.InsertStatement) => MetaMemberKind.InsertStatement,
                    nameof(meta.Proceed) => MetaMemberKind.Proceed,
                    nameof(meta.ProceedAsync) => MetaMemberKind.ProceedAsync,
                    nameof(meta.ProceedEnumerable) => MetaMemberKind.ProceedEnumerable,
                    nameof(meta.ProceedEnumerator) => MetaMemberKind.ProceedEnumerator,
                    "ProceedAsyncEnumerable" => MetaMemberKind.ProceedAsyncEnumerable,
                    "ProceedAsyncEnumerator" => MetaMemberKind.ProceedAsyncEnumerator,
                    nameof(meta.InvokeTemplate) => MetaMemberKind.InvokeTemplate,
                    nameof(meta.Return) => MetaMemberKind.Return,
                    nameof(meta.DefineLocalVariable) => MetaMemberKind.DefineLocalVariable,
                    nameof(meta.RunTime) => MetaMemberKind.RunTime,
                    _ => MetaMemberKind.Default
                };
            }
            else
            {
                return MetaMemberKind.None;
            }
        }
    }
}