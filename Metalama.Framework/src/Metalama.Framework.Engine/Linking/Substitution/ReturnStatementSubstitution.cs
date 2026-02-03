// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes the return statement based on current inlining context.
/// </summary>
internal sealed class ReturnStatementSubstitution : SyntaxNodeSubstitution
{
    private readonly IMethodSymbol _referencingSymbol;
    private readonly IMethodSymbol _originalContainingSymbol;
    private readonly string? _returnVariableIdentifier;
    private readonly string? _returnLabelIdentifier;
    private readonly bool _replaceByBreakIfOmitted;

    public override SyntaxNode ReplacedNode { get; }

    public ReturnStatementSubstitution(
        CompilationContext compilationContext,
        SyntaxNode returnNode,
        IMethodSymbol referencingSymbol,
        IMethodSymbol containingSymbol,
        string? returnVariableIdentifier,
        string? returnLabelIdentifier,
        bool replaceByBreakIfOmitted ) : base( compilationContext )
    {
        this.ReplacedNode = returnNode;
        this._referencingSymbol = referencingSymbol;
        this._originalContainingSymbol = containingSymbol;
        this._returnVariableIdentifier = returnVariableIdentifier;
        this._returnLabelIdentifier = returnLabelIdentifier;
        this._replaceByBreakIfOmitted = replaceByBreakIfOmitted;
    }

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        var syntaxGenerator = substitutionContext.SyntaxGenerationContext.SyntaxGenerator;

        switch ( currentNode )
        {
            case ReturnStatementSyntax returnStatement:
                if ( this._returnLabelIdentifier != null )
                {
                    if ( returnStatement.Expression != null )
                    {
                        return
                            syntaxGenerator.FormattedBlock(
                                    CreateAssignmentStatement( returnStatement.Expression )
                                        .WithTriviaFromIfNecessary( returnStatement, substitutionContext.SyntaxGenerationContext.Options )
                                        .WithOriginalLocationAnnotationFrom( returnStatement ),
                                    CreateGotoStatement() )
                                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                    }
                    else
                    {
                        return CreateGotoStatement();
                    }
                }
                else
                {
                    if ( returnStatement.Expression != null )
                    {
                        var assignmentStatement =
                            CreateAssignmentStatement( returnStatement.Expression )
                                .WithTriviaFromIfNecessary( returnStatement, substitutionContext.SyntaxGenerationContext.Options )
                                .WithOriginalLocationAnnotationFrom( returnStatement );

                        if ( this._replaceByBreakIfOmitted )
                        {
                            return
                                syntaxGenerator.FormattedBlock(
                                        assignmentStatement,
                                        BreakStatement(
                                            Token( SyntaxKind.BreakKeyword ),
                                            Token(
                                                TriviaList(),
                                                SyntaxKind.SemicolonToken,
                                                substitutionContext.SyntaxGenerationContext.OptionalElasticEndOfLineTriviaList ) ) )
                                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                        }
                        else
                        {
                            return assignmentStatement;
                        }
                    }
                    else
                    {
                        if ( this._replaceByBreakIfOmitted )
                        {
                            return
                                BreakStatement(
                                        Token( SyntaxKind.BreakKeyword ),
                                        Token(
                                            TriviaList(),
                                            SyntaxKind.SemicolonToken,
                                            substitutionContext.SyntaxGenerationContext.OptionalElasticEndOfLineTriviaList ) )
                                    .WithOriginalLocationAnnotationFrom( returnStatement );
                        }
                        else
                        {
                            return EmptyStatement()
                                .WithOriginalLocationAnnotationFrom( returnStatement )
                                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.EmptyTriviaStatement );
                        }
                    }
                }

            case ExpressionSyntax returnExpression:
                if ( this._returnLabelIdentifier != null )
                {
                    if ( this._referencingSymbol.ReturnsVoid )
                    {
                        return
                            syntaxGenerator.FormattedBlock(
                                    ExpressionStatement( returnExpression ).WithOriginalLocationAnnotationFrom( returnExpression ),
                                    CreateGotoStatement() )
                                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                    }
                    else
                    {
                        return
                            syntaxGenerator.FormattedBlock(
                                    CreateAssignmentStatement( returnExpression ).WithOriginalLocationAnnotationFrom( returnExpression ),
                                    CreateGotoStatement() )
                                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                    }
                }
                else
                {
                    var assignmentStatement =
                        CreateAssignmentStatement( returnExpression )
                            .WithOriginalLocationAnnotationFrom( returnExpression );

                    if ( this._replaceByBreakIfOmitted )
                    {
                        return
                            syntaxGenerator.FormattedBlock(
                                    assignmentStatement,
                                    BreakStatement(
                                        Token( SyntaxKind.BreakKeyword ),
                                        Token(
                                            TriviaList(),
                                            SyntaxKind.SemicolonToken,
                                            substitutionContext.SyntaxGenerationContext.OptionalElasticEndOfLineTriviaList ) ) )
                                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                    }
                    else
                    {
                        return assignmentStatement;
                    }
                }

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }

        StatementSyntax CreateAssignmentStatement( ExpressionSyntax expression )
        {
            IdentifierNameSyntax identifier;

            if ( this._returnVariableIdentifier != null )
            {
                identifier = SyntaxFactoryEx.SafeIdentifierName( this._returnVariableIdentifier );
            }
            else
            {
                // For async methods (with the async modifier), use the result type (the inner type
                // of Task<T>/ValueTask<T>) instead of the full return type, because when inlining
                // with await, the return expression should have the awaited type.
                // For non-async methods that return Task<T>/ValueTask<T>, we must use the full
                // return type since their return expressions are of type Task<T>.
                var returnType = this._originalContainingSymbol.ReturnType;

                if ( this._originalContainingSymbol.IsAsyncSafe() &&
                     AsyncHelper.TryGetAsyncInfo( returnType, out var resultType, out _ ) )
                {
                    returnType = resultType;
                }

                // For void-returning methods (including void-like async methods like Task/ValueTask),
                // we can't assign to a discard, so just use the expression as a statement.
                if ( returnType.SpecialType == SpecialType.System_Void )
                {
                    return ExpressionStatement( expression )
                        .WithOptionalTrailingLineFeed( substitutionContext.SyntaxGenerationContext );
                }

                identifier = SyntaxFactoryEx.DiscardIdentifierName();

                expression = syntaxGenerator.SafeCastExpression(
                    syntaxGenerator.TypeSyntax( returnType ),
                    expression );
            }

            return SyntaxFactoryEx.AssignmentStatement( identifier, expression, substitutionContext.SyntaxGenerationContext );
        }

        GotoStatementSyntax CreateGotoStatement()
        {
            return
                GotoStatement(
                        SyntaxKind.GotoStatement,
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.GotoKeyword ),
                        default,
                        SyntaxFactoryEx.SafeIdentifierName( this._returnLabelIdentifier.AssertNotNull() ),
                        Token( default, SyntaxKind.SemicolonToken, substitutionContext.SyntaxGenerationContext.OptionalElasticEndOfLineTriviaList ) )
                    .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
        }
    }
}