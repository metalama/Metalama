// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Reflection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal static class SyntaxUtility
    {
        public static ExpressionSyntax CreateBindingFlags( IMember member, SyntaxSerializationContext serializationContext )
        {
            return new[] { member.Accessibility == Accessibility.Public ? "Public" : "NonPublic", member.IsStatic ? "Static" : "Instance" }
                .SelectAsReadOnlyList(
                    f => (ExpressionSyntax) MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        serializationContext.GetTypeSyntax( typeof(BindingFlags) ),
                        SyntaxFactoryEx.WellKnownIdentifierName( f ) ) )
                .Aggregate( ( l, r ) => BinaryExpression( SyntaxKind.BitwiseOrExpression, l, r ) );
        }

        /// <summary>
        /// Creates a <c>expression ?? throw new ExceptionType("message")</c> coalesce expression that throws a helpful exception
        /// when a reflection lookup returns null (e.g., due to linker trimming or obfuscation).
        /// </summary>
        public static ExpressionSyntax CoalesceWithMissingMemberException(
            ExpressionSyntax expression,
            Type exceptionType,
            string memberDisplayString,
            string memberKind,
            SyntaxSerializationContext serializationContext )
        {
            var message = $"The {memberKind} '{memberDisplayString}' could not be found using reflection.";

            var throwExpression = ThrowExpression(
                ObjectCreationExpression( serializationContext.SyntaxGenerator.TypeSyntax( serializationContext.GetTypeSymbol( exceptionType ) ) )
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal( message ) ) ) ) ) ) );

            // Wrap in parentheses to ensure correct precedence when the result is used with member access
            // (e.g., .ReturnParameter, .GetParameters()[0]).
            return ParenthesizedExpression( BinaryExpression( SyntaxKind.CoalesceExpression, expression, throwExpression ) );
        }
    }
}