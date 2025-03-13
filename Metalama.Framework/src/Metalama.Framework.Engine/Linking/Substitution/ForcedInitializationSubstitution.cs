// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Comparers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitution of the constructor body that adds initial forced initialization statements to avoid warnings/errors.
/// </summary>
internal sealed class ForcedInitializationSubstitution : SyntaxNodeSubstitution
{
    private readonly SyntaxNode _rootNode;
    private readonly IReadOnlyList<ISymbol> _symbolsToInitialize;

    public ForcedInitializationSubstitution( CompilationContext compilationContext, SyntaxNode rootNode, IReadOnlyList<ISymbol> symbolsToInitialize ) : base(
        compilationContext )
    {
        this._rootNode = rootNode;
        this._symbolsToInitialize = symbolsToInitialize;
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        // We currently need to support all name syntaxes that may reference a property of the current object.

        switch ( currentNode )
        {
            case BlockSyntax block:
                return
                    block.WithStatements( block.Statements.InsertRange( 0, GetInitializationStatements() ) );

            case ArrowExpressionClauseSyntax arrowExpressionClause:
                return
                    Block( List( GetInitializationStatements().Append( ExpressionStatement( arrowExpressionClause.Expression ) ) ) );

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }

        IEnumerable<StatementSyntax> GetInitializationStatements()
        {
            foreach ( var symbol in this._symbolsToInitialize.OrderBy( static x => x, StructuralSymbolComparer.Default ) )
            {
                yield return
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                ThisExpression(),
                                IdentifierName( LinkerRewritingDriver.GetBackingFieldName( symbol ) ) ),
                            LiteralExpression(
                                SyntaxKind.DefaultLiteralExpression,
                                Token( SyntaxKind.DefaultKeyword ) ) ) );
            }
        }
    }
}