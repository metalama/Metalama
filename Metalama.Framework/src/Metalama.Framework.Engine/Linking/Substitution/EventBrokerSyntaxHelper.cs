// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Formatting;
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
    public static ExpressionSyntax GetEventBrokerField( string eventBrokerFieldName, bool isStatic )
    {
        if ( isStatic )
        {
            return SyntaxFactoryEx.WellKnownIdentifierName( eventBrokerFieldName );
        }
        else
        {
            return MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ThisExpression(),
                SyntaxFactoryEx.WellKnownIdentifierName( eventBrokerFieldName ) );
        }
    }

    /// <summary>
    /// Creates the syntax for event broker adder method body.
    /// </summary>
    public static BlockSyntax CreateAddHandlerBody(
        SyntaxGenerationContext context,
        string eventBrokerFieldName,
        ExpressionSyntax fieldInitializationExpression,
        bool isStatic )
    {
        return
            context.SyntaxGenerator.FormattedBlock(
                ExpressionStatement( fieldInitializationExpression )
                    .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ),
                ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                GetEventBrokerField( eventBrokerFieldName, isStatic ),
                                SyntaxFactoryEx.WellKnownIdentifierName( "AddHandler" ) ),
                            ArgumentList( SingletonSeparatedList( Argument( SyntaxFactoryEx.WellKnownIdentifierName( "value" ) ) ) ) ) )
                    .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ) );
    }

    /// <summary>
    /// Creates the syntax for event broker remover method body.
    /// </summary>
    public static BlockSyntax CreateRemoveHandlerBody(
        SyntaxGenerationContext context,
        string eventBrokerFieldName,
        bool isStatic )
    {
        return
            context.SyntaxGenerator.FormattedBlock(
                ExpressionStatement(
                        ConditionalAccessExpression(
                            GetEventBrokerField( eventBrokerFieldName, isStatic ),
                            InvocationExpression(
                                MemberBindingExpression( SyntaxFactoryEx.WellKnownIdentifierName( "RemoveHandler" ) ),
                                ArgumentList( SingletonSeparatedList( Argument( SyntaxFactoryEx.WellKnownIdentifierName( "value" ) ) ) ) ) ) )
                    .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ) );
    }
}