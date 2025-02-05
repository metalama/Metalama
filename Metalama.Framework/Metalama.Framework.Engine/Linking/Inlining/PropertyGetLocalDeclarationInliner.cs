// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Inlining
{
    internal sealed class PropertyGetLocalDeclarationInliner : PropertyGetInliner
    {
        public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
        {
            if ( !base.CanInline( aspectReference, semanticModel ) )
            {
                return false;
            }

            // The syntax has to be in form: <type> <local> = <annotated_property_expression>;
            if ( aspectReference.ResolvedSemantic.Symbol is not IPropertySymbol
                 && (aspectReference.ResolvedSemantic.Symbol as IMethodSymbol)?.AssociatedSymbol is not IPropertySymbol )
            {
                // Coverage: ignore (hit only when the check in base class is incorrect).
                return false;
            }

            var propertySymbol =
                aspectReference.ResolvedSemantic.Symbol as IPropertySymbol
                ?? (IPropertySymbol) ((aspectReference.ResolvedSemantic.Symbol as IMethodSymbol)?.AssociatedSymbol).AssertNotNull();

            // Should be within equals clause.
            if ( aspectReference.RootExpression.Parent is not EqualsValueClauseSyntax equalsClause )
            {
                return false;
            }

            // Should be within variable declarator.
            if ( equalsClause.Parent is not VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax variableDeclaration } )
            {
                // Coverage: ignore (only incorrect code can get here).
                return false;
            }

            // Should be single-variable declaration.
            if ( variableDeclaration.Variables.Count != 1 )
            {
                return false;
            }

            // Variable and property type should be equal (i.e. no implicit conversions).
            if ( !SymbolEqualityComparer.Default.Equals( semanticModel.GetSymbolInfo( variableDeclaration.Type ).Symbol, propertySymbol.Type ) )
            {
                return false;
            }

            // Should be within local declaration.
            if ( variableDeclaration.Parent is not LocalDeclarationStatementSyntax )
            {
                // Coverage: ignore (only incorrect code can get here).
                return false;
            }

            return true;
        }

        public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
        {
            var equalsClause = (EqualsValueClauseSyntax) aspectReference.RootExpression.Parent.AssertNotNull();
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