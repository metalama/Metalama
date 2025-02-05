// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

internal sealed class BlockWithReturnBeforeUsingLocalSubstitution : SyntaxNodeSubstitution
{
    private BlockSyntax RootBlock { get; }

    public BlockWithReturnBeforeUsingLocalSubstitution( CompilationContext compilationContext, BlockSyntax rootBlock ) : base( compilationContext )
    {
        this.RootBlock = rootBlock;
    }

    public override SyntaxNode ReplacedNode => this.RootBlock;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        switch ( currentNode )
        {
            case BlockSyntax rootBlock:
                var gotoStatementWalker = new GotoAndLabeledStatementWalker();

                // PERF: Visits already inlined bodies (which may have been processed by another instance), leading to O(n^2) time complexity in extreme cases.
                gotoStatementWalker.Visit( rootBlock );

                var containedLabels =
                    gotoStatementWalker.LabeledStatements.SelectAsReadOnlyList( x => x.Identifier.Text ).ToHashSet();

                var gotoStatements =
                    gotoStatementWalker.GotoStatements
                        .Where( g => g.Expression is IdentifierNameSyntax identifierName && !containedLabels.Contains( identifierName.Identifier.Text ) )
                        .ToArray();

                var statementsContainingOutgoingGoto = GetStatementsContainingOutgoingGotoStatement( rootBlock, gotoStatements );

                var encounteredStatementContainingGotoStatement = false;
                var tailStatements = new List<StatementSyntax>();
                var segments = new List<(List<StatementSyntax> Statements, LocalDeclarationStatementSyntax Using)>();

                foreach ( var statement in rootBlock.Statements )
                {
                    if ( statementsContainingOutgoingGoto.Contains( statement ) )
                    {
                        encounteredStatementContainingGotoStatement = true;
                    }

                    if ( statement is LocalDeclarationStatementSyntax localDeclaration
                         && localDeclaration.UsingKeyword != default
                         && encounteredStatementContainingGotoStatement )
                    {
                        segments.Add( (tailStatements, localDeclaration) );
                        tailStatements = new List<StatementSyntax>();
                        encounteredStatementContainingGotoStatement = false;
                    }
                    else
                    {
                        tailStatements.Add( statement );
                    }
                }

                if ( segments.Count == 0 )
                {
                    return currentNode;
                }
                else
                {
                    var currentBlock = Block( tailStatements );

                    for ( var i = segments.Count - 1; i >= 0; i-- )
                    {
                        currentBlock =
                            Block( List( segments[i].Statements.Append( Translate( segments[i].Using, currentBlock.Statements ) ) ) );
                    }

                    return rootBlock.WithStatements( currentBlock.Statements );
                }

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }

        UsingStatementSyntax Translate( LocalDeclarationStatementSyntax local, IEnumerable<StatementSyntax> statements )
        {
            return
                UsingStatement(
                    Token( local.UsingKeyword.LeadingTrivia, SyntaxKind.UsingKeyword, local.UsingKeyword.TrailingTrivia ),
                    Token( TriviaList( ElasticMarker ), SyntaxKind.OpenParenToken, TriviaList( ElasticMarker ) ),
                    local.Declaration,
                    null,
                    Token(
                        TriviaList( ElasticMarker ),
                        SyntaxKind.CloseParenToken,
                        substitutionContext.SyntaxGenerationContext.ElasticEndOfLineTriviaList ),
                    Block(
                        Token( local.SemicolonToken.LeadingTrivia, SyntaxKind.OpenBraceToken, local.SemicolonToken.TrailingTrivia ),
                        List( statements ),
                        Token(
                            TriviaList( ElasticSpace ),
                            SyntaxKind.CloseBraceToken,
                            substitutionContext.SyntaxGenerationContext.ElasticEndOfLineTriviaList ) ) );
        }

        static HashSet<StatementSyntax> GetStatementsContainingOutgoingGotoStatement(
            BlockSyntax rootBlock,
            IReadOnlyList<GotoStatementSyntax> gotoStatements )
        {
            var statementsContainingGotoStatement = new HashSet<StatementSyntax>();

            foreach ( var gotoStatement in gotoStatements )
            {
                Mark( gotoStatement );

                void Mark( SyntaxNode node )
                {
                    if ( node == rootBlock )
                    {
                        statementsContainingGotoStatement.Add( rootBlock );

                        return;
                    }

                    if ( node is StatementSyntax statement )
                    {
                        if ( statementsContainingGotoStatement.Add( statement ) && statement != rootBlock )
                        {
                            // Process recursively unvisited statement that is not the root block.
                            Mark( statement.Parent.AssertNotNull() );
                        }
                    }
                    else
                    {
                        // Process recursively the parent of a non-statement.
                        Mark( node.Parent.AssertNotNull() );
                    }
                }
            }

            return statementsContainingGotoStatement;
        }
    }

    private sealed class GotoAndLabeledStatementWalker : CSharpSyntaxWalker
    {
        private int _blockDepth = -1;

        public List<GotoStatementSyntax> GotoStatements { get; }

        public List<LabeledStatementSyntax> LabeledStatements { get; }

        public GotoAndLabeledStatementWalker()
        {
            this.GotoStatements = new List<GotoStatementSyntax>();
            this.LabeledStatements = new List<LabeledStatementSyntax>();
        }

        public override void VisitGotoStatement( GotoStatementSyntax node ) => this.GotoStatements.Add( node );

        public override void VisitLabeledStatement( LabeledStatementSyntax node )
        {
            if ( this._blockDepth > 0 )
            {
                // Add only labels that are declared deeper in the root block.
                this.LabeledStatements.Add( node );
            }
        }

        public override void VisitBlock( BlockSyntax node )
        {
            try
            {
                this._blockDepth++;

                base.VisitBlock( node );
            }
            finally
            {
                this._blockDepth--;
            }
        }

        public override void Visit( SyntaxNode? node )
        {
            if ( node is not ExpressionSyntax and not LocalFunctionStatementSyntax )
            {
                // Skip expressions and local functions.
                base.Visit( node );
            }
        }
    }
}