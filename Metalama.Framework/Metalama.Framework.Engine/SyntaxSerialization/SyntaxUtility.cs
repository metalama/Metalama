// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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