// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class CharSerializer : ObjectSerializer<char>
    {
        public override ExpressionSyntax Serialize( char obj, SyntaxSerializationContext serializationContext )
        {
            return LiteralExpression( SyntaxKind.CharacterLiteralExpression, Literal( obj ) );
        }

        public CharSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}