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
/// Substitutes accesses to event field delegate, i.e. the backing field.
/// </summary>
internal sealed class EventRaiseBrokerSubstitution : SyntaxNodeSubstitution
{
    private readonly SyntaxNode _rootNode;
    private readonly IMethodSymbol _containingMethod;

    public EventRaiseBrokerSubstitution( CompilationContext compilationContext, SyntaxNode rootNode, IMethodSymbol containingMethod ) : base( compilationContext )
    {
        this._rootNode = rootNode;
        this._containingMethod = containingMethod;
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        switch ( currentNode )
        {
            case ExpressionSyntax expression:
                // Look at the signature of the containing method.
                // We will replace with handler( args.Field1, ...,  args.Fieldn). args is always a value tuple.
                Invariant.Assert( this._containingMethod.Parameters is [{ Type: { TypeKind: TypeKind.Delegate } }, { Type: INamedTypeSymbol { TupleUnderlyingType: not null } }] );

                var handlerName = this._containingMethod.Parameters[0].Name;
                var argsName = this._containingMethod.Parameters[1].Name;
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

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }
    }
}