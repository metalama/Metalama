// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerLinkingStep
{
    private sealed class RemoveTrailingReturnRewriter( SyntaxGenerationContext generationContext ) : SafeSyntaxRewriter
    {
        public override SyntaxNode VisitBlock( BlockSyntax node )
        {
            if ( node.Statements.Count > 0 && node.Statements.Last() is ReturnStatementSyntax { Expression: null } )
            {
                // Count return statements to be removed.
                var returnStatementsToRemove = 0;
                var preserveTrivia = false;

                for ( var i = node.Statements.Count - 1; i >= 0; i-- )
                {
                    if ( node.Statements[i] is ReturnStatementSyntax { Expression: null } returnStatement )
                    {
                        returnStatementsToRemove++;
                        preserveTrivia = preserveTrivia || SyntaxExtensions.ShouldTriviaBePreserved( returnStatement, generationContext.Options );
                    }
                    else
                    {
                        break;
                    }
                }

                var newStatements = new List<StatementSyntax>( node.Statements.Count - returnStatementsToRemove );

                // Copy everything before the return statement.
                for ( var i = 0; i < node.Statements.Count - returnStatementsToRemove; i++ )
                {
                    newStatements.Add( node.Statements[i] );
                }

                if ( preserveTrivia )
                {
                    var additionalTrailingTrivia = new List<SyntaxTrivia>();

                    for ( var i = node.Statements.Count - returnStatementsToRemove; i < node.Statements.Count; i++ )
                    {
                        additionalTrailingTrivia.AddRange( node.Statements[i].GetLeadingTrivia() );
                        additionalTrailingTrivia.AddRange( node.Statements[i].GetTrailingTrivia().StripFirstTrailingNewLine() );
                    }

                    additionalTrailingTrivia.AddRange( node.CloseBraceToken.LeadingTrivia );

                    return node.PartialUpdate(
                        statements: List( newStatements ),
                        closeBraceToken: node.CloseBraceToken.WithRequiredLeadingTrivia( additionalTrailingTrivia ) );
                }
                else
                {
                    return node.WithStatements( List( newStatements ) );
                }
            }
            else
            {
                return node;
            }
        }
    }
}