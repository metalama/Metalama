// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Inlining
{
    internal sealed class MethodLocalDeclarationInliner : MethodInliner
    {
        public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
        {
            if ( !base.CanInline( aspectReference, semanticModel ) )
            {
                return false;
            }

            // The syntax has to be in form: <type> <local> = <annotated_method_expression>( <arguments> );
            if ( aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol methodSymbol )
            {
                // Coverage: ignore (hit only when the check in base class is incorrect).
                return false;
            }

            // Should be within invocation expression.
            if ( aspectReference.RootExpression.AssertNotNull().Parent is not InvocationExpressionSyntax invocationExpression )
            {
                return false;
            }

            // Should be within equals clause.
            if ( invocationExpression.Parent is not EqualsValueClauseSyntax equalsClause )
            {
                return false;
            }

            // Should be within variable declarator.
            if ( equalsClause.Parent is not VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax variableDeclaration } )
            {
                // Only incorrect code can get here.
                throw new AssertionFailedException( Justifications.CoverageMissing );

                // return false;
            }

            // Should be single-variable declaration.
            if ( variableDeclaration.Variables.Count != 1 )
            {
                return false;
            }

            // Variable and method return type should be equal (i.e. no implicit conversions).
            if ( !SignatureTypeComparer.Instance.Equals(
                    semanticModel.GetSymbolInfo( variableDeclaration.Type ).Symbol,
                    methodSymbol.ReturnType ) )
            {
                return false;
            }

            // Should be within local declaration.
            if ( variableDeclaration.Parent is not LocalDeclarationStatementSyntax )
            {
                return false;
            }

            // The invocation needs to be inlineable in itself.
            if ( !IsInlineableInvocation( semanticModel, aspectReference.ContainingSemantic.Symbol, invocationExpression ) )
            {
                return false;
            }

            return true;
        }

        public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
        {
            var invocationExpression = (InvocationExpressionSyntax) aspectReference.RootExpression.AssertNotNull().Parent.AssertNotNull();
            var equalsClause = (EqualsValueClauseSyntax) invocationExpression.Parent.AssertNotNull();
            var variableDeclarator = (VariableDeclaratorSyntax) equalsClause.Parent.AssertNotNull();
            var variableDeclaration = (VariableDeclarationSyntax) variableDeclarator.Parent.AssertNotNull();
            var localDeclaration = (LocalDeclarationStatementSyntax) variableDeclaration.Parent.AssertNotNull();

            return new InliningAnalysisInfo( localDeclaration, variableDeclarator.Identifier.Text );
        }

        public override StatementSyntax Inline(
            SyntaxGenerationContext syntaxGenerationContext,
            InliningSpecification specification,
            SyntaxNode currentNode,
            StatementSyntax linkedTargetBody )
        {
            if ( currentNode is not StatementSyntax currentStatement )
            {
                throw new AssertionFailedException( $"The node is not expected to be a statement: {currentNode}" );
            }

            return syntaxGenerationContext.SyntaxGenerator.FormattedBlock(
                    LocalDeclarationStatement(
                            VariableDeclaration(
                                syntaxGenerationContext.SyntaxGenerator.TypeSyntax( specification.DestinationSemantic.Symbol.ReturnType ),
                                SingletonSeparatedList( VariableDeclarator( Identifier( specification.ReturnVariableIdentifier.AssertNotNull() ) ) ) ) )
                        .NormalizeWhitespaceIfNecessary( syntaxGenerationContext )
                        .WithOptionalTrailingLineFeed( syntaxGenerationContext ),
                    linkedTargetBody )
                .WithFormattingAnnotationsFrom( currentStatement )
                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock )
                .AddTriviaFromIfNecessary( currentNode, syntaxGenerationContext.Options );
        }
    }
}