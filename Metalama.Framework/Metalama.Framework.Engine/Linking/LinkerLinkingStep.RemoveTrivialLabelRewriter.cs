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
    // this rewriter is temporary until we properly use results of Control Flow Analysis while inlining.
    private sealed class RemoveTrivialLabelRewriter( IReadOnlyDictionary<string, int> observedLabelCounter, SyntaxGenerationContext generationContext )
        : SafeSyntaxRewriter
    {
        public override SyntaxNode VisitBlock( BlockSyntax node )
        {
            var newStatements = new List<StatementSyntax>();
            var anyChange = false;

            var nextStatement = node.Statements.Count > 0 ? (StatementSyntax?) this.Visit( node.Statements[0] ) : null;

            if ( node.Statements.Count > 0 && nextStatement != node.Statements[0] )
            {
                anyChange = true;
            }

            for ( var i = 0; i < node.Statements.Count; i++ )
            {
                var currentStatement = nextStatement;
                nextStatement = i + 1 < node.Statements.Count ? (StatementSyntax?) this.Visit( node.Statements[i + 1] ) : null;

                if ( i + 1 < node.Statements.Count && nextStatement != node.Statements[i + 1] )
                {
                    anyChange = true;
                }

                if ( currentStatement == null )
                {
                    continue;
                }

                if ( currentStatement is GotoStatementSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: var gotoLabel } } gotoStatement
                     && nextStatement is LabeledStatementSyntax { Identifier.ValueText: var declaredLabel } labeledStatement
                     && gotoLabel == declaredLabel
                     && observedLabelCounter.TryGetValue( declaredLabel, out var counter )
                     && counter == 1 )
                {
                    if ( SyntaxExtensions.ShouldTriviaBePreserved( gotoStatement, generationContext.Options )
                         || SyntaxExtensions.ShouldTriviaBePreserved( labeledStatement, generationContext.Options ) )
                    {
                        List<SyntaxTrivia> trivia =
                        [
                            ElasticMarker,
                            .. gotoStatement.GetLeadingTrivia(),
                            .. gotoStatement.GetTrailingTrivia(),
                            ElasticMarker,
                            .. labeledStatement.GetLeadingTrivia(),
                            .. labeledStatement.Statement.GetLeadingTrivia()
                        ];

                        newStatements.Add( labeledStatement.Statement.WithRequiredLeadingTrivia( trivia ) );
                    }
                    else
                    {
                        newStatements.Add( labeledStatement.Statement );
                    }

                    anyChange = true;
                    nextStatement = null;

                    continue;
                }

                newStatements.Add( currentStatement );
            }

            if ( anyChange )
            {
                return node.WithStatements( List( newStatements ) );
            }
            else
            {
                return node;
            }
        }
    }
}