// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Handles inlining of local declaration with await expression: var x = await M(); or var x = (await M());
/// </summary>
internal sealed class AwaitLocalDeclarationInliner : AsyncMethodInliner
{
    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !base.CanInline( aspectReference, semanticModel ) )
        {
            return false;
        }

        // The syntax has to be in form: <type> <local> = await <annotated_method_expression>( <arguments> );
        // or: <type> <local> = (await <annotated_method_expression>( <arguments> ));
        if ( aspectReference.ResolvedSemantic.Symbol.Kind != SymbolKind.Method || aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol methodSymbol )
        {
            return false;
        }

        if ( !aspectReference.RootExpression.AssertNotNull().Parent.IsKind( SyntaxKind.InvocationExpression )
             || aspectReference.RootExpression.AssertNotNull().Parent is not InvocationExpressionSyntax invocationExpression )
        {
            return false;
        }

        // The invocation should be inside an await expression, possibly through parentheses.
        var possibleAwait = InlinerHelper.SkipParenthesizedExpressionAncestors( invocationExpression ).Parent;

        if ( !possibleAwait.IsKind( SyntaxKind.AwaitExpression ) || possibleAwait is not AwaitExpressionSyntax awaitExpression )
        {
            return false;
        }

        // The await expression (possibly parenthesized) should be inside an equals value clause.
        var awaitOrParenthesized = InlinerHelper.SkipParenthesizedExpressionAncestors( awaitExpression );

        if ( !awaitOrParenthesized.Parent.IsKind( SyntaxKind.EqualsValueClause ) || awaitOrParenthesized.Parent is not EqualsValueClauseSyntax equalsClause )
        {
            return false;
        }

        // Should be within variable declarator.
        if ( equalsClause.Parent is not VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax variableDeclaration } )
        {
            throw new AssertionFailedException( Justifications.CoverageMissing );
        }

        // Should be single-variable declaration.
        if ( variableDeclaration.Variables.Count != 1 )
        {
            return false;
        }

        // Get the awaited type (the T in Task<T>/ValueTask<T>/custom awaitable).
        if ( !AsyncHelper.TryGetAsyncInfo( methodSymbol.ReturnType, out var awaitedType, out _ ) )
        {
            return false;
        }

        // Variable and awaited return type should be equal (i.e. no implicit conversions).
        // Use GetDeclaredSymbol to handle 'var' declarations where GetSymbolInfo returns null.
        var localSymbol = semanticModel.GetDeclaredSymbol( variableDeclaration.Variables[0] ) as ILocalSymbol;
        var localType = localSymbol?.Type ?? semanticModel.GetTypeInfo( variableDeclaration.Type ).Type;

        if ( localType == null || !SignatureTypeComparer.Instance.Equals( localType, awaitedType ) )
        {
            return false;
        }

        // Should be within local declaration.
        if ( !variableDeclaration.Parent.IsKind( SyntaxKind.LocalDeclarationStatement ) || variableDeclaration.Parent is not LocalDeclarationStatementSyntax )
        {
            return false;
        }

        // The invocation needs to be inlineable in itself.
        if ( !InlinerHelper.IsCanonicalInvocation( semanticModel, aspectReference.ContainingSemantic.Symbol, invocationExpression ) )
        {
            return false;
        }

        return true;
    }

    public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
    {
        var invocationExpression = (InvocationExpressionSyntax) aspectReference.RootExpression.AssertNotNull().Parent.AssertNotNull();

        // Navigate through parentheses to find the await expression.
        var awaitExpression = (AwaitExpressionSyntax) InlinerHelper.SkipParenthesizedExpressionAncestors( invocationExpression ).Parent.AssertNotNull();

        // Navigate through parentheses to find the equals clause.
        var current = InlinerHelper.SkipParenthesizedExpressionAncestors( awaitExpression );

        var equalsClause = (EqualsValueClauseSyntax) current.Parent.AssertNotNull();
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

        // Get the awaited type from the destination method's return type.
        var methodReturnType = specification.DestinationSemantic.Symbol.ReturnType;

        if ( !AsyncHelper.TryGetAsyncInfo( methodReturnType, out var awaitedType, out _ ) )
        {
            throw new AssertionFailedException( $"Could not get awaited type from {methodReturnType}" );
        }

        return syntaxGenerationContext.SyntaxGenerator.FormattedBlock(
                LocalDeclarationStatement(
                        VariableDeclaration(
                            syntaxGenerationContext.SyntaxGenerator.TypeSyntax( awaitedType ),
                            SingletonSeparatedList(
                                VariableDeclarator( SyntaxFactoryEx.SafeIdentifier( specification.ReturnVariableIdentifier.AssertNotNull() ) ) ) ) )
                    .NormalizeWhitespaceIfNecessary( syntaxGenerationContext )
                    .WithOptionalTrailingLineFeed( syntaxGenerationContext ),
                linkedTargetBody )
            .WithFormattingAnnotationsFrom( currentStatement )
            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock )
            .AddTriviaFromIfNecessary( currentNode, syntaxGenerationContext.Options );
    }
}