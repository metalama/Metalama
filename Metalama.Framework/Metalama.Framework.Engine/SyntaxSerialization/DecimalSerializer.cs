// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class DecimalSerializer : ObjectSerializer<decimal>
    {
        public override ExpressionSyntax Serialize( decimal obj, SyntaxSerializationContext serializationContext )
        {
            return SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal( obj ) );
        }

        public DecimalSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}