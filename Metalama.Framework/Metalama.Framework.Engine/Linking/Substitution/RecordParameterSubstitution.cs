// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

internal sealed class RecordParameterSubstitution : SyntaxNodeSubstitution
{
    private readonly ParameterSyntax _rootNode;
    private readonly IMethodSymbol _targetAccessor;
    private readonly string? _returnVariableIdentifier;

    public RecordParameterSubstitution(
        CompilationContext compilationContext,
        ParameterSyntax rootNode,
        IMethodSymbol targetAccessor,
        string? returnVariableIdentifier )
        : base( compilationContext )
    {
        this._rootNode = rootNode;
        this._targetAccessor = targetAccessor;
        this._returnVariableIdentifier = returnVariableIdentifier;
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        var syntaxGenerator = substitutionContext.SyntaxGenerationContext.SyntaxGenerator;

        switch (currentNode, this._targetAccessor.MethodKind)
        {
            case (ParameterSyntax, MethodKind.PropertyGet):
                if ( this._returnVariableIdentifier != null )
                {
                    return
                        syntaxGenerator.FormattedBlock(
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName( this._returnVariableIdentifier ),
                                        CreateFieldAccessExpression() ) ) )
                            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                }
                else
                {
                    return
                        syntaxGenerator.FormattedBlock(
                                ReturnStatement(
                                    Token( TriviaList(), SyntaxKind.ReturnKeyword, TriviaList( ElasticSpace ) ),
                                    CreateFieldAccessExpression(),
                                    Token(
                                        TriviaList(),
                                        SyntaxKind.SemicolonToken,
                                        substitutionContext.SyntaxGenerationContext.ElasticEndOfLineTriviaList ) ) )
                            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                }

            case (ParameterSyntax, MethodKind.PropertySet):
                return
                    syntaxGenerator.FormattedBlock(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    CreateFieldAccessExpression(),
                                    IdentifierName( "value" ) ) ) )
                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );

            default:
                throw new AssertionFailedException( $"Unsupported combination: {currentNode}, {this._targetAccessor}" );
        }

        ExpressionSyntax CreateFieldAccessExpression()
        {
            return
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName( LinkerRewritingDriver.GetBackingFieldName( (IPropertySymbol) this._targetAccessor.AssociatedSymbol.AssertNotNull() ) ) );
        }
    }
}