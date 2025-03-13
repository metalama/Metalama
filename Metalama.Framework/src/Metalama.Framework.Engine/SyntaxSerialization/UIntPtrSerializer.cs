// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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