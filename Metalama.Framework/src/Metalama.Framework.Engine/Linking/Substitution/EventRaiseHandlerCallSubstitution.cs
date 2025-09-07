// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes handler invocation within the raise override method (this calls the `handler` argument using `args` argument).
/// </summary>
internal sealed class EventRaiseHandlerCallSubstitution : SyntaxNodeSubstitution
{
    private readonly SyntaxNode _rootNode;
    private readonly IMethodSymbol _containingMethod;

    public EventRaiseHandlerCallSubstitution( CompilationContext compilationContext, ResolvedAspectReference aspectReference ) : base( compilationContext )
    {
        this._rootNode = aspectReference.RootNode;
        this._containingMethod = aspectReference.ContainingBody;
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        switch ( currentNode )
        {
            case InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: LinkerInjectionHelperProvider.HelperTypeName },
                    Name: IdentifierNameSyntax { Identifier.ValueText: LinkerInjectionHelperProvider.EventRaiseMemberName }
                },
                ArgumentList.Arguments:
                [
                    {
                        Expression: ParenthesizedLambdaExpressionSyntax
                        {
                            ExpressionBody: AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax eventMemberAccess }
                        }
                    },
                    ..
                ] arguments 
            }:
                Invariant.Assert( this._containingMethod.Parameters is [{ Type: { TypeKind: TypeKind.Delegate } }, { Type: INamedTypeSymbol { TupleUnderlyingType: not null } }] );

                var handlerName = this._containingMethod.Parameters[0].Name;
                var argsName = this._containingMethod.Parameters[1].Name;

                if ( arguments.Count == 1 )
                {
                    // This is meta.Proceed, which does not specify arguments (or a parameter-less delegate event).
                    var tupleElements = ((INamedTypeSymbol) this._containingMethod.Parameters[1].Type).TupleElements;

                    return
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName( handlerName ),
                                IdentifierName( "Invoke" ) ),
                            ArgumentList(
                                SeparatedList(
                                    tupleElements.Select( e =>
                                        Argument(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName( argsName ),
                                                IdentifierName( e.Name ) ) ) ) ) ) );
                }
                else
                {
                    // This is invoker call, which specifies argument expressions.
                    return
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName( handlerName ),
                                IdentifierName( "Invoke" ) ),
                            ArgumentList(
                                SeparatedList(
                                arguments.Skip( 1 ).Select(
                                    a => Argument( a.Expression ) ) ) ) );
                }

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }
    }
}