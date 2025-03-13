// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal static class SyntaxUtility
    {
        public static ExpressionSyntax CreateBindingFlags( IMember member, SyntaxSerializationContext serializationContext )
        {
            return new[] { member.Accessibility == Accessibility.Public ? "Public" : "NonPublic", member.IsStatic ? "Static" : "Instance" }
                .SelectAsReadOnlyList(
                    f => (ExpressionSyntax) SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        serializationContext.GetTypeSyntax( typeof(BindingFlags) ),
                        SyntaxFactory.IdentifierName( f ) ) )
                .Aggregate( ( l, r ) => SyntaxFactory.BinaryExpression( SyntaxKind.BitwiseOrExpression, l, r ) );
        }
    }
}