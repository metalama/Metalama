// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Provides common helper methods for generating event broker syntax.
/// </summary>
internal static class EventBrokerSyntaxHelper
{
    /// <summary>
    /// Creates the syntax for event broker adder method body.
    /// </summary>
    public static BlockSyntax CreateAddHandlerBody(
        SyntaxGenerationContext context,
        string eventBrokerFieldName,
        ExpressionSyntax fieldInitializationExpression )
    {
        return
            context.SyntaxGenerator.FormattedBlock(
                ExpressionStatement( fieldInitializationExpression ),
                ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                ThisExpression(),
                                IdentifierName( eventBrokerFieldName ) ),
                            IdentifierName( "AddHandler" ) ),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    IdentifierName( "value" ) ) ) ) ) ) );
    }

    /// <summary>
    /// Creates the syntax for event broker remover method body.
    /// </summary>
    public static BlockSyntax CreateRemoveHandlerBody(
        SyntaxGenerationContext context,
        string eventBrokerFieldName )
    {
        return
            context.SyntaxGenerator.FormattedBlock(
                ExpressionStatement(
                    ConditionalAccessExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName( eventBrokerFieldName ) ),
                        InvocationExpression(
                            MemberBindingExpression(
                                IdentifierName( "RemoveHandler" ) ),
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        IdentifierName( "value" ) ) ) ) ) ) ) );
    }
}