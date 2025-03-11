// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal static class TypeSerializationHelper
    {
        public static ExpressionSyntax SerializeTypeSymbolRecursive( ITypeSymbol symbol, SyntaxSerializationContext serializationContext )
        {
            switch ( symbol )
            {
                case ITypeParameterSymbol:
                    // Serializing a generic parameter always assume that we are in a lexical scope where
                    // the symbol exists. Getting the generic parameter e.g. using typeof(X).GetGenericArguments()[Y]
                    // is not supported and would require an API change.
                    return TypeOfExpression( IdentifierName( symbol.Name ) );

                default:
                    return SerializeTypeFromSymbolLeaf( symbol, serializationContext );
            }
        }

        private static ExpressionSyntax SerializeTypeFromSymbolLeaf( ITypeSymbol typeSymbol, SyntaxSerializationContext serializationContext )
        {
            // We always use typeof, regardless of the type accessibility. This means that the type must be accessible from the calling
            // context, but this is a reasonable assumption.
            return serializationContext.SyntaxGenerator.TypeOfExpression( typeSymbol );
        }
    }
}