// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;

namespace Metalama.Framework.Engine.Formatting
{
    public partial class FormattedCodeWriter
    {
        private sealed class AddTitlesVisitor : SafeSyntaxWalker
        {
            private readonly ClassifiedTextSpanCollection _textSpans;
            private readonly SemanticModel _semanticModel;
            private readonly CompilationContext _compilationContext;

            public AddTitlesVisitor( ClassifiedTextSpanCollection textSpans, SemanticModel semanticModel )
            {
                this._textSpans = textSpans;
                this._semanticModel = semanticModel;
                this._compilationContext = semanticModel.Compilation.GetCompilationContext();
            }

            private void ProcessNode<T>( T node, TextSpan span )
                where T : SyntaxNode
            {
                if ( string.Equals( node.ToString(), "var", StringComparison.Ordinal ) )
                {
                    return;
                }

                var symbolInfo = this._semanticModel.GetSymbolInfo( node );

                var symbol = symbolInfo.Symbol ?? this._semanticModel.GetDeclaredSymbol( node );

                if ( symbol != null )
                {
                    var doc = XmlDocumentationReader.Instance.GetFormattedDocumentation( symbol, this._compilationContext );

                    if ( doc != null )
                    {
                        this._textSpans.SetTag( span, "title", doc );
                    }
                }
            }

            public override void VisitMethodDeclaration( MethodDeclarationSyntax node )
            {
                this.ProcessNode( node, node.Identifier.Span );
                base.VisitMethodDeclaration( node );
            }

            public override void VisitGenericName( GenericNameSyntax node ) => this.ProcessNode( node, node.Identifier.Span );

            public override void VisitIdentifierName( IdentifierNameSyntax node ) => this.ProcessNode( node, node.Span );
        }
    }
}