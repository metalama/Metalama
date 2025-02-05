// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class IntPtrSerializer : ObjectSerializer<IntPtr>
    {
        public override ExpressionSyntax Serialize( IntPtr obj, SyntaxSerializationContext serializationContext )
        {
            return SyntaxFactory.ObjectCreationExpression( serializationContext.GetTypeSyntax( typeof(IntPtr) ) )
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal( obj.ToInt64() ) ) ) );
        }

        public IntPtrSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}