// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxExtensions = Metalama.Framework.Engine.Utilities.Roslyn.SyntaxExtensions;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerLinkingStep
{
    private sealed class CleanupBodyRewriter( SyntaxGenerationContext generationContext ) : SafeSyntaxRewriter
    {
        // TODO: Optimize (this reallocates multiple times).

        public override SyntaxNode VisitBlock( BlockSyntax node )
        {
            this.TransformStatementList( node.Statements, out var anyRewrittenStatement, out var finalStatements, out var overflowingTrivia );

            if ( overflowingTrivia.ShouldBePreserved( generationContext.Options ) )
            {
                if ( finalStatements.Count > 0 )
                {
                    finalStatements[^1] = finalStatements[^1]
                        .WithRequiredTrailingTrivia( finalStatements[^1].GetTrailingTrivia().AddRange( overflowingTrivia ) );
                }
                else
                {
                    node = node.WithCloseBraceToken(
                        node.CloseBraceToken.WithRequiredLeadingTrivia( overflowingTrivia.AddRange( node.CloseBraceToken.LeadingTrivia ) ) );
                }
            }

            if ( anyRewrittenStatement )
            {
                return node.Update( this.VisitToken( node.OpenBraceToken ), List( finalStatements ), this.VisitToken( node.CloseBraceToken ) );
            }
            else
            {
                return node.Update( this.VisitToken( node.OpenBraceToken ), node.Statements, this.VisitToken( node.CloseBraceToken ) );
            }
        }

        public override SyntaxNode VisitSwitchSection( SwitchSectionSyntax node )
        {
            this.TransformStatementList( node.Statements, out var anyRewrittenStatement, out var finalStatements, out var overflowingTrivia );

            if ( overflowingTrivia.ShouldBePreserved( generationContext.Options ) )
            {
                if ( finalStatements.Count > 0 )
                {
                    finalStatements[^1] = finalStatements[^1]
                        .WithRequiredTrailingTrivia( finalStatements[^1].GetTrailingTrivia().AddRange( overflowingTrivia ) );
                }
                else
                {
                    throw new AssertionFailedException( $"No final statement for switch section: {node}." );
                }
            }

            if ( anyRewrittenStatement )
            {
                return node.Update( this.VisitList( node.Labels ), List( finalStatements ) );
            }
            else
            {
                return node.Update( this.VisitList( node.Labels ), node.Statements );
            }
        }

        private void TransformStatementList(
            SyntaxList<StatementSyntax> originalStatements,
            out bool anyRewrittenStatement,
            out List<StatementSyntax> finalStatements,
            out SyntaxTriviaList overflowingTrivia )
        {
            anyRewrittenStatement = false;
            var newStatements = new List<StatementSyntax>();

            foreach ( var statement in originalStatements )
            {
                if ( statement.Kind() == SyntaxKind.Block && statement is BlockSyntax innerBlock )
                {
                    var innerBlockFlags = innerBlock.GetLinkerGeneratedFlags();

                    if ( innerBlockFlags.HasFlagFast( LinkerGeneratedFlags.FlattenableBlock ) )
                    {
                        anyRewrittenStatement = true;

                        AddFlattenedBlockStatements( innerBlock, newStatements );
                    }
                    else
                    {
                        var rewritten = this.VisitBlock( innerBlock );

                        if ( rewritten != statement )
                        {
                            anyRewrittenStatement = true;
                        }

                        newStatements.Add( (StatementSyntax) rewritten.AssertNotNull() );
                    }
                }
                else
                {
                    var rewritten = this.Visit( statement );

                    if ( rewritten != statement )
                    {
                        anyRewrittenStatement = true;
                    }

                    if ( rewritten != null )
                    {
                        newStatements.Add( (StatementSyntax) rewritten.AssertNotNull() );
                    }
                }
            }

            finalStatements = new List<StatementSyntax>();
            overflowingTrivia = SyntaxTriviaList.Empty;

            // Process statements, cleaning empty labeled statements, and trivia empty statements and invocations with empty empty expressions.
            for ( var i = 0; i < newStatements.Count; i++ )
            {
                var statement = newStatements[i];

                if ( statement.GetLinkerGeneratedFlags().HasFlagFast( LinkerGeneratedFlags.EmptyLabeledStatement ) )
                {
                    var labeledStatement = (LabeledStatementSyntax) statement;

                    if ( i == newStatements.Count - 1 )
                    {
                        finalStatements.Add( labeledStatement );
                    }
                    else
                    {
                        finalStatements.Add(
                            LabeledStatement(
                                labeledStatement.Identifier.WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ),
                                Token( SyntaxKind.ColonToken ).WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ),
                                newStatements[i + 1] ) );

                        i++;
                    }

                    anyRewrittenStatement = true;
                }
                else if ( statement.GetLinkerGeneratedFlags().HasFlagFast( LinkerGeneratedFlags.EmptyTriviaStatement ) )
                {
                    var leadingTrivia = statement.GetLeadingTrivia();
                    var trailingTrivia = statement.GetTrailingTrivia();

                    if ( leadingTrivia.ShouldBePreserved( generationContext.Options ) || trailingTrivia.ShouldBePreserved( generationContext.Options ) )
                    {
                        // This is statement that carries only trivias and should be removed, trivias added to the previous and next statement.
                        if ( finalStatements.Count == 0 )
                        {
                            // There is not yet any statement to attach the trivia so everything goes into overflow.
                            overflowingTrivia = overflowingTrivia.AddRange( leadingTrivia ).AddRange( trailingTrivia );
                        }
                        else
                        {
                            // We need to replace trailing trivia of the last statement.
                            var newTrailingTrivia =
                                overflowingTrivia.Count > 0
                                    ? finalStatements[^1].GetTrailingTrivia()
                                        .AddRange( overflowingTrivia )
                                        .AddRange( leadingTrivia )
                                    : finalStatements[^1].GetTrailingTrivia().AddRange( leadingTrivia );

                            finalStatements[^1] = finalStatements[^1].WithRequiredTrailingTrivia( newTrailingTrivia );

                            overflowingTrivia = trailingTrivia.StripFirstTrailingNewLine();
                        }
                    }

                    anyRewrittenStatement = true;
                }
                else
                {
                    finalStatements.Add( statement );
                }
            }

            void AddFlattenedBlockStatements( BlockSyntax block, List<StatementSyntax> statements )
            {
                // Remember the predicted index of the first statement in the inlined block, which will receive trivia from open brace token.
                var firstStatementIndex = statements.Count;

                foreach ( var statement in block.Statements )
                {
                    if ( statement.Kind() == SyntaxKind.Block && statement is BlockSyntax innerBlock && innerBlock.GetLinkerGeneratedFlags().HasFlagFast( LinkerGeneratedFlags.FlattenableBlock ) )
                    {
                        AddFlattenedBlockStatements( innerBlock, statements );
                    }
                    else
                    {
                        var visitedStatement = (StatementSyntax?) this.Visit( statement );

                        if ( visitedStatement != null )
                        {
                            statements.Add( visitedStatement.WithFormattingAnnotationsFrom( block ) );
                        }
                    }
                }

                // Index of the last statement.
                var lastStatementIndex = statements.Count - 1;

                if ( SyntaxExtensions.ShouldTriviaBePreserved( block.OpenBraceToken, generationContext.Options )
                     || SyntaxExtensions.ShouldTriviaBePreserved( block.CloseBraceToken, generationContext.Options ) )
                {
                    var openBraceTrailingTrivia = block.OpenBraceToken.TrailingTrivia.StripFirstTrailingNewLine();

                    // If the first statement's leading trivia contains a comment (non-whitespace, non-EOL trivia),
                    // we need to preserve the newline from the open brace to avoid placing the comment on the same line as '{'.
                    if ( firstStatementIndex < statements.Count )
                    {
                        var firstStatementLeading = statements[firstStatementIndex].GetLeadingTrivia();

                        if ( HasCommentTrivia( firstStatementLeading ) )
                        {
                            openBraceTrailingTrivia = block.OpenBraceToken.TrailingTrivia;
                        }
                    }

                    var leadingTrivia = block.OpenBraceToken.LeadingTrivia.AddRange( openBraceTrailingTrivia );
                    var trailingTrivia = block.CloseBraceToken.LeadingTrivia.AddRange( block.CloseBraceToken.TrailingTrivia.StripFirstTrailingNewLine() );

                    if ( firstStatementIndex >= statements.Count )
                    {
                        // There was no statement added.
                        // We will add an empty statement to carry trivia, which we will prune above.
                        statements.Add(
                            EmptyStatement( Token( leadingTrivia, SyntaxKind.SemicolonToken, trailingTrivia ) )
                                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.EmptyTriviaStatement ) );
                    }
                    else
                    {
                        statements[firstStatementIndex] =
                            statements[firstStatementIndex]
                                .WithRequiredLeadingTrivia( leadingTrivia.AddRange( statements[firstStatementIndex].GetLeadingTrivia() ) );

                        statements[lastStatementIndex] =
                            statements[lastStatementIndex]
                                .WithRequiredTrailingTrivia( statements[lastStatementIndex].GetTrailingTrivia().AddRange( trailingTrivia ) );
                    }
                }

                static bool HasCommentTrivia( SyntaxTriviaList triviaList )
                {
                    foreach ( var trivia in triviaList )
                    {
                        if ( trivia.IsKind( SyntaxKind.SingleLineCommentTrivia )
                             || trivia.IsKind( SyntaxKind.MultiLineCommentTrivia )
                             || trivia.IsKind( SyntaxKind.SingleLineDocumentationCommentTrivia )
                             || trivia.IsKind( SyntaxKind.MultiLineDocumentationCommentTrivia ) )
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }

        public override SyntaxNode VisitInvocationExpression( InvocationExpressionSyntax node )
        {
            if ( node.Expression.GetLinkerGeneratedFlags().HasFlagFast( LinkerGeneratedFlags.NullAspectReferenceExpression ) )
            {
                return IdentifierName( "__LINKER_TO_BE_REMOVED__" )
                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.NullAspectReferenceExpression );
            }

            return node;
        }

        public override SyntaxNode? VisitExpressionStatement( ExpressionStatementSyntax node )
        {
            var transformed = (ExpressionSyntax) this.Visit( node.Expression ).AssertNotNull();

            if ( transformed.GetLinkerGeneratedFlags().HasFlagFast( LinkerGeneratedFlags.NullAspectReferenceExpression ) )
            {
                return null;
            }

            return node.Update( transformed, this.VisitToken( node.SemicolonToken ) );
        }

        public override SyntaxToken VisitToken( SyntaxToken token )
        {
            token = base.VisitToken( token );

            if ( TryFilterTriviaList( token.LeadingTrivia, out var filteredLeadingTrivia ) )
            {
                token = token.WithRequiredLeadingTrivia( filteredLeadingTrivia );
            }

            if ( TryFilterTriviaList( token.TrailingTrivia, out var filteredTrailingTrivia ) )
            {
                token = token.WithRequiredTrailingTrivia( filteredTrailingTrivia );
            }

            return token;

            static bool TryFilterTriviaList( SyntaxTriviaList triviaList, out SyntaxTriviaList filteredTriviaList )
            {
                var anyChange = false;

                foreach ( var trivia in triviaList )
                {
                    if ( trivia.GetLinkerGeneratedFlags().HasFlagFast( LinkerGeneratedFlags.GeneratedSuppression ) )
                    {
                        anyChange = true;

                        break;
                    }
                }

                if ( anyChange )
                {
                    var triviaBuilder = new List<SyntaxTrivia>( triviaList.Count );

                    foreach ( var trivia in triviaList )
                    {
                        if ( !trivia.GetLinkerGeneratedFlags().HasFlagFast( LinkerGeneratedFlags.GeneratedSuppression ) )
                        {
                            triviaBuilder.Add( trivia );
                        }
                    }

                    filteredTriviaList = new SyntaxTriviaList( triviaBuilder );

                    return true;
                }
                else
                {
                    filteredTriviaList = triviaList;

                    return false;
                }
            }
        }
    }
}