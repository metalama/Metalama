// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class UIntSerializer : ObjectSerializer<uint>
    {
        public override ExpressionSyntax Serialize( uint obj, SyntaxSerializationContext serializationContext )
        {
            return SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal( obj ) );
        }

        public UIntSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}