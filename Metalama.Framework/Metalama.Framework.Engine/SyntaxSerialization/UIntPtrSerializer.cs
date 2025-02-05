// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class UIntPtrSerializer : ObjectSerializer<UIntPtr>
    {
        public override ExpressionSyntax Serialize( UIntPtr obj, SyntaxSerializationContext serializationContext )
        {
            return SyntaxFactory.ObjectCreationExpression( serializationContext.GetTypeSyntax( typeof(UIntPtr) ) )
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal( obj.ToUInt64() ) ) ) );
        }

        public UIntPtrSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}