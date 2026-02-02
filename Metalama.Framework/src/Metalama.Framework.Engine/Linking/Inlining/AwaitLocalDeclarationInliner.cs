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
        if ( aspectReference.ResolvedSemantic.Symbol is not IMethodSymbol methodSymbol )
        {
            return false;
        }

        if ( aspectReference.RootExpression.AssertNotNull().Parent is not InvocationExpressionSyntax invocationExpression )
        {
            return false;
        }

        // The invocation should be inside an await expression.
        if ( invocationExpression.Parent is not AwaitExpressionSyntax awaitExpression )
        {
            return false;
        }

        // The await expression (possibly parenthesized) should be inside an equals value clause.
        SyntaxNode awaitOrParenthesized = awaitExpression;

        while ( awaitOrParenthesized.Parent is ParenthesizedExpressionSyntax )
        {
            awaitOrParenthesized = awaitOrParenthesized.Parent;
        }

        if ( awaitOrParenthesized.Parent is not EqualsValueClauseSyntax equalsClause )
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

        // Get the awaited type (the T in Task<T>/ValueTask<T>).
        var awaitedType = GetAwaitedType( methodSymbol.ReturnType );

        if ( awaitedType is null )
        {
            return false;
        }

        // Variable and awaited return type should be equal (i.e. no implicit conversions).
        if ( !SignatureTypeComparer.Instance.Equals( semanticModel.GetSymbolInfo( variableDeclaration.Type ).Symbol, awaitedType ) )
        {
            return false;
        }

        // Should be within local declaration.
        if ( variableDeclaration.Parent is not LocalDeclarationStatementSyntax )
        {
            return false;
        }

        // The invocation needs to be inlineable in itself.
        if ( !IsCanonicalInvocation( semanticModel, aspectReference.ContainingSemantic.Symbol, invocationExpression ) )
        {
            return false;
        }

        return true;
    }

    public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
    {
        var invocationExpression = (InvocationExpressionSyntax) aspectReference.RootExpression.AssertNotNull().Parent.AssertNotNull();
        var awaitExpression = (AwaitExpressionSyntax) invocationExpression.Parent.AssertNotNull();

        // Navigate through parentheses.
        SyntaxNode current = awaitExpression;

        while ( current.Parent is ParenthesizedExpressionSyntax )
        {
            current = current.Parent;
        }

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
        var awaitedType = GetAwaitedType( methodReturnType );

        if ( awaitedType is null )
        {
            throw new AssertionFailedException( $"Could not get awaited type from {methodReturnType}" );
        }

        return syntaxGenerationContext.SyntaxGenerator.FormattedBlock(
                LocalDeclarationStatement(
                        VariableDeclaration(
                            syntaxGenerationContext.SyntaxGenerator.TypeSyntax( awaitedType ),
                            SingletonSeparatedList( VariableDeclarator( SyntaxFactoryEx.SafeIdentifier( specification.ReturnVariableIdentifier.AssertNotNull() ) ) ) ) )
                    .NormalizeWhitespaceIfNecessary( syntaxGenerationContext )
                    .WithOptionalTrailingLineFeed( syntaxGenerationContext ),
                linkedTargetBody )
            .WithFormattingAnnotationsFrom( currentStatement )
            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock )
            .AddTriviaFromIfNecessary( currentNode, syntaxGenerationContext.Options );
    }

    private static ITypeSymbol? GetAwaitedType( ITypeSymbol returnType )
    {
        // For Task<T>, ValueTask<T>, get T.
        // For Task, ValueTask, return null (void).
        if ( returnType is INamedTypeSymbol namedType )
        {
            var fullName = namedType.OriginalDefinition.ToDisplayString();

            if ( fullName is "System.Threading.Tasks.Task<TResult>" or "System.Threading.Tasks.ValueTask<TResult>" )
            {
                return namedType.TypeArguments[0];
            }
        }

        return null;
    }
}
