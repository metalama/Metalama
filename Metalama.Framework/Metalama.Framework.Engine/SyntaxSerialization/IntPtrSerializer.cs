// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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