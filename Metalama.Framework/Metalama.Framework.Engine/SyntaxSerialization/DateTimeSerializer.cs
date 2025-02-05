// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class DateTimeSerializer : ObjectSerializer<DateTime>
    {
        public override ExpressionSyntax Serialize( DateTime obj, SyntaxSerializationContext serializationContext )
        {
            return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        serializationContext.GetTypeSyntax( typeof(DateTime) ),
                        IdentifierName( "FromBinary" ) ) )
                .AddArgumentListArguments(
                    Argument(
                        LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal( obj.ToBinary() ) ) ) );
        }

        public DateTimeSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}