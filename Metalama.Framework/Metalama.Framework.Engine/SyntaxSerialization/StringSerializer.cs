// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class StringSerializer : ObjectSerializer<string>
    {
        public override ExpressionSyntax Serialize( string obj, SyntaxSerializationContext serializationContext )
        {
            return SyntaxFactory.LiteralExpression( SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal( obj ) );
        }

        public StringSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}