// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class BoolSerializer : ObjectSerializer<bool>
    {
        public override ExpressionSyntax Serialize( bool obj, SyntaxSerializationContext serializationContext )
        {
            return LiteralExpression( obj ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression );
        }

        public BoolSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}