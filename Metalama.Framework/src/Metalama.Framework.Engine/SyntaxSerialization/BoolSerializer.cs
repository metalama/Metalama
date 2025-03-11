// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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