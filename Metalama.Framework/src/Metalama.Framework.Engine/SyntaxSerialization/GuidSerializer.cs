// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class GuidSerializer : ObjectSerializer<Guid>
    {
        public override ExpressionSyntax Serialize( Guid obj, SyntaxSerializationContext serializationContext )
        {
            var b = obj.ToByteArray();

            var a = (b[3] << 24) | (b[2] << 16) | (b[1] << 8) | b[0];
            var b2 = (short) ((b[5] << 8) | b[4]);
            var c = (short) ((b[7] << 8) | b[6]);
            var d = b[8];
            var e = b[9];
            var f = b[10];
            var g = b[11];
            var h = b[12];
            var i = b[13];
            var j = b[14];
            var k = b[15];

            return ObjectCreationExpression( serializationContext.GetTypeSyntax( typeof(Guid) ) )
                .AddArgumentListArguments(
                    Argument( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( a ) ) ),
                    Argument( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( b2 ) ) ),
                    Argument( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( c ) ) ),
                    Argument( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( d ) ) ),
                    Argument( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( e ) ) ),
                    Argument( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( f ) ) ),
                    Argument( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( g ) ) ),
                    Argument( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( h ) ) ),
                    Argument( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( i ) ) ),
                    Argument( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( j ) ) ),
                    Argument( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( k ) ) ) )
                .NormalizeWhitespaceIfNecessary( serializationContext.SyntaxGenerationContext );
        }

        public GuidSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}