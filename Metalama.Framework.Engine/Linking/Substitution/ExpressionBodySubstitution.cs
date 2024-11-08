// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution
{
    internal sealed class ExpressionBodySubstitution : SyntaxNodeSubstitution
    {
        private readonly ArrowExpressionClauseSyntax _rootNode;
        private readonly IMethodSymbol _referencingSymbol;
        private readonly IMethodSymbol _originalContainingSymbol;
        private readonly bool _usingSimpleInlining;
        private readonly string? _returnVariableIdentifier;

        public ExpressionBodySubstitution(
            CompilationContext compilationContext,
            ArrowExpressionClauseSyntax rootNode,
            IMethodSymbol referencingSymbol,
            IMethodSymbol originalContainingSymbol,
            bool usingSimpleInlining,
            string? returnVariableIdentifier = null ) : base( compilationContext )
        {
            Invariant.Implies( usingSimpleInlining, returnVariableIdentifier == null );

            Invariant.Implies(
                usingSimpleInlining,
                StructuralSymbolComparer.Signature.Equals( referencingSymbol.ReturnType, originalContainingSymbol.ReturnType ) );

            Invariant.Implies( originalContainingSymbol.ReturnsVoid, this._returnVariableIdentifier == null );

            this._rootNode = rootNode;
            this._referencingSymbol = referencingSymbol;
            this._originalContainingSymbol = originalContainingSymbol;
            this._usingSimpleInlining = usingSimpleInlining;
            this._returnVariableIdentifier = returnVariableIdentifier;
        }

        public override SyntaxNode ReplacedNode => this._rootNode;

        public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
        {
            var syntaxGenerator = substitutionContext.SyntaxGenerationContext.SyntaxGenerator;

            switch ( currentNode )
            {
                case ArrowExpressionClauseSyntax { Expression: ThrowExpressionSyntax throwExpressionSyntax }:
                    {
                        return
                            syntaxGenerator.FormattedBlock(
                                    ThrowStatement(
                                        throwExpressionSyntax.ThrowKeyword,
                                        throwExpressionSyntax.Expression,
                                        Token( SyntaxKind.SemicolonToken ) ) )
                                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                    }

                case ArrowExpressionClauseSyntax arrowExpressionClause:
                    if ( this._usingSimpleInlining )
                    {
                        // Uses the simple inlining, i.e. generating simple return statement without any changes for non-void methods.
                        if ( this._referencingSymbol.ReturnsVoid )
                        {
                            return
                                syntaxGenerator.FormattedBlock( ExpressionStatement( arrowExpressionClause.Expression ) )
                                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                        }
                        else
                        {
                            return
                                syntaxGenerator.FormattedBlock(
                                        ReturnStatement(
                                            Token( arrowExpressionClause.Expression.GetLeadingTrivia(), SyntaxKind.ReturnKeyword, TriviaList( Space ) ),
                                            arrowExpressionClause.Expression,
                                            Token(
                                                TriviaList(),
                                                SyntaxKind.SemicolonToken,
                                                arrowExpressionClause.Expression.GetTrailingTrivia()
                                                    .AddOptionalLineFeed( substitutionContext.SyntaxGenerationContext ) ) ) )
                                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                        }
                    }
                    else
                    {
                        if ( this._referencingSymbol.ReturnsVoid )
                        {
                            if ( this._originalContainingSymbol.ReturnsVoid )
                            {
                                // Both referencing and target methods return void, expression can be simply changed to an expression statement.

                                return
                                    syntaxGenerator.FormattedBlock( ExpressionStatement( arrowExpressionClause.Expression ) )
                                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                            }
                            else
                            {
                                return DiscardBlock();
                            }
                        }
                        else
                        {
                            if ( this._returnVariableIdentifier != null )
                            {
                                return syntaxGenerator.FormattedBlock(
                                        SyntaxFactoryEx.AssignmentStatement(
                                            IdentifierName( this._returnVariableIdentifier ),
                                            arrowExpressionClause.Expression,
                                            syntaxGenerator.SyntaxGenerationContext ) )
                                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                            }
                            else
                            {
                                if ( this._originalContainingSymbol.ReturnsVoid )
                                {
                                    return
                                        syntaxGenerator.FormattedBlock( ExpressionStatement( arrowExpressionClause.Expression ) )
                                            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                                }
                                else
                                {
                                    return DiscardBlock();
                                }
                            }
                        }

                        BlockSyntax DiscardBlock()
                        {
                            var returnTypeSyntax =
                                substitutionContext.SyntaxGenerationContext.SyntaxGenerator.TypeSyntax( this._originalContainingSymbol.ReturnType );

                            return syntaxGenerator.FormattedBlock(
                                    SyntaxFactoryEx.DiscardStatement(
                                        syntaxGenerator.SafeCastExpression( returnTypeSyntax, arrowExpressionClause.Expression ) ) )
                                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                        }
                    }

                default:
                    throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
            }
        }
    }
}