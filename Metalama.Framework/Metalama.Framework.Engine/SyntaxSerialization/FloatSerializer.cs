// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class FloatSerializer : ObjectSerializer<float>
    {
        public override ExpressionSyntax Serialize( float obj, SyntaxSerializationContext serializationContext )
        {
            if ( float.IsPositiveInfinity( obj ) )
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PredefinedType( SyntaxFactory.Token( SyntaxKind.FloatKeyword ) ),
                    SyntaxFactory.IdentifierName( "PositiveInfinity" ) );
            }

            if ( float.IsNegativeInfinity( obj ) )
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PredefinedType( SyntaxFactory.Token( SyntaxKind.FloatKeyword ) ),
                    SyntaxFactory.IdentifierName( "NegativeInfinity" ) );
            }

            if ( float.IsNaN( obj ) )
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PredefinedType( SyntaxFactory.Token( SyntaxKind.FloatKeyword ) ),
                    SyntaxFactory.IdentifierName( "NaN" ) );
            }

            return SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal( obj ) );
        }

        public FloatSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}