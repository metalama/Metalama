// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Templating
{
    /// <summary>
    /// A <see cref="CSharpSyntaxRewriter"/> that flattens blocks from the output of <see cref="TemplateCompiler"/>.
    /// Some blocks must be flattened for semantic reasons, other just for aesthetic ones.
    /// </summary>
    internal sealed class FlattenBlocksRewriter : SafeSyntaxRewriter
    {
        public override SyntaxNode VisitBlock( BlockSyntax node )
        {
            // This flattens the block structure when possible (i.e. there is no local variable)
            // and when it is requested through an annotation.

            var previousTrivia = SyntaxTriviaList.Empty;
            var statements = new List<StatementSyntax>();

            foreach ( var statement in node.Statements )
            {
                var processedStatement = (StatementSyntax) this.Visit( statement )!;

                switch ( processedStatement )
                {
                    case EmptyStatementSyntax emptyStatement:
                        // Empty statements are to be removed, but trivia needs to be preserved.
                        previousTrivia =
                            previousTrivia
                                .AddRange( emptyStatement.SemicolonToken.LeadingTrivia )
                                .AddRange( emptyStatement.SemicolonToken.TrailingTrivia );

                        continue;

                    case BlockSyntax innerBlock:
                        {
                            // This block was already processed - it does not contain empty statements and nested blocks that can be flattened.
                            var mustFlatten = innerBlock.HasFlattenBlockAnnotation();

                            if ( mustFlatten || !innerBlock.Statements.Any( s => s is LocalDeclarationStatementSyntax ) )
                            {
                                previousTrivia =
                                    previousTrivia
                                        .AddRange( innerBlock.OpenBraceToken.LeadingTrivia )
                                        .AddRange( innerBlock.OpenBraceToken.TrailingTrivia );

                                foreach ( var innerStatement in innerBlock.Statements )
                                {
                                    statements.Add(
                                        innerStatement
                                            .WithLeadingTrivia( previousTrivia.AddRange( innerStatement.GetLeadingTrivia() ) ) );

                                    previousTrivia = SyntaxTriviaList.Empty;
                                }

                                previousTrivia =
                                    previousTrivia
                                        .AddRange( innerBlock.CloseBraceToken.LeadingTrivia )
                                        .AddRange( innerBlock.CloseBraceToken.TrailingTrivia );
                            }
                            else
                            {
                                statements.Add(
                                    innerBlock
                                        .WithLeadingTrivia( previousTrivia.AddRange( processedStatement.GetLeadingTrivia() ) ) );

                                previousTrivia = SyntaxTriviaList.Empty;
                            }

                            break;
                        }

                    default:
                        statements.Add(
                            processedStatement
                                .WithLeadingTrivia( previousTrivia.AddRange( processedStatement.GetLeadingTrivia() ) ) );

                        previousTrivia = SyntaxTriviaList.Empty;

                        break;
                }
            }

            return node
                .WithStatements( List( statements ) )
                .WithCloseBraceToken( node.CloseBraceToken.WithLeadingTrivia( previousTrivia.AddRange( node.CloseBraceToken.LeadingTrivia ) ) );
        }
    }
}