// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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