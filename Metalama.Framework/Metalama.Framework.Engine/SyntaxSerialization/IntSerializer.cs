// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class IntSerializer : ObjectSerializer<int>
    {
        public override ExpressionSyntax Serialize( int obj, SyntaxSerializationContext serializationContext )
        {
            return LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( obj ) );
        }

        public IntSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}