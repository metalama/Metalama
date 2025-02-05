// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class DateTimeOffsetSerializer : ObjectSerializer<DateTimeOffset>
    {
        public override ExpressionSyntax Serialize( DateTimeOffset obj, SyntaxSerializationContext serializationContext )
        {
            var isoTime = obj.ToString( "o" );

            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    serializationContext.GetTypeSyntax( typeof(DateTimeOffset) ),
                    IdentifierName( "Parse" ) ),
                ArgumentList(
                    SingletonSeparatedList(
                        Argument(
                            LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal( isoTime ) ) ) ) ) );
        }

        public DateTimeOffsetSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}