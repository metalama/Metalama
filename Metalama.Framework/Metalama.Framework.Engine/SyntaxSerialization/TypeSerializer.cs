// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class TypeSerializer : ObjectSerializer<Type>
    {
        public override ExpressionSyntax Serialize( Type obj, SyntaxSerializationContext serializationContext )
            => TypeSerializationHelper.SerializeTypeSymbolRecursive( serializationContext.GetTypeSymbol( obj ), serializationContext );

        public TypeSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}