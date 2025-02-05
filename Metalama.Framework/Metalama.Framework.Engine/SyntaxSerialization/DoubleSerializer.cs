// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class DoubleSerializer : ObjectSerializer<double>
    {
        public override ExpressionSyntax Serialize( double obj, SyntaxSerializationContext serializationContext )
        {
            if ( double.IsPositiveInfinity( obj ) )
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PredefinedType( SyntaxFactory.Token( SyntaxKind.DoubleKeyword ) ),
                    SyntaxFactory.IdentifierName( "PositiveInfinity" ) );
            }

            if ( double.IsNegativeInfinity( obj ) )
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PredefinedType( SyntaxFactory.Token( SyntaxKind.DoubleKeyword ) ),
                    SyntaxFactory.IdentifierName( "NegativeInfinity" ) );
            }

            if ( double.IsNaN( obj ) )
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PredefinedType( SyntaxFactory.Token( SyntaxKind.DoubleKeyword ) ),
                    SyntaxFactory.IdentifierName( "NaN" ) );
            }

            return SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal( obj ) );
        }

        public DoubleSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}