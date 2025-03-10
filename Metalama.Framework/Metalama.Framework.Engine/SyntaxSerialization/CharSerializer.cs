// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class CharSerializer : ObjectSerializer<char>
    {
        public override ExpressionSyntax Serialize( char obj, SyntaxSerializationContext serializationContext )
        {
            return LiteralExpression( SyntaxKind.CharacterLiteralExpression, Literal( obj ) );
        }

        public CharSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}