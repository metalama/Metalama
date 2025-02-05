// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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