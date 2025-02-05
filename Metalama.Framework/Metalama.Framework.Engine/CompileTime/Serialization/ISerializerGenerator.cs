// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.CompileTime.Serialization
{
    internal interface ISerializerGenerator
    {
        bool ShouldSuppressReadOnly( SerializableTypeInfo serializableType, ISymbol memberSymbol );

        MemberDeclarationSyntax? CreateDeserializingConstructor( SerializableTypeInfo serializableType, SyntaxToken constructorName );

        TypeDeclarationSyntax? CreateSerializerType( SerializableTypeInfo serializedType, TypeSyntax serializedTypeSyntax );
    }
}