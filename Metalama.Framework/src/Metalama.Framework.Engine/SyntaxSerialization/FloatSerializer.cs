// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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