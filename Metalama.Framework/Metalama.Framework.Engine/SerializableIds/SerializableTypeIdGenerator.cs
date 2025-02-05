// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.SerializableIds;

public static class SerializableTypeIdGenerator
{
    public static SerializableTypeId GetSerializableTypeId( this ITypeSymbol symbol, bool includeGenericContext = false )
    {
        var id = SyntaxGenerationContext.Contextless.SyntaxGenerator.TypeSyntax( symbol ).ToString();

        if ( symbol.NullableAnnotation != NullableAnnotation.None )
        {
            id += '!';
        }

        id = SerializableTypeId.Prefix + id;

        if ( includeGenericContext )
        {
            var genericContext = TypeParameterSymbolDetector.GetTypeContext( symbol );

            if ( genericContext != null )
            {
                // If there is a reference to a type parameter, we must append its context.
                var contextId = genericContext.GetSerializableId().Id;
                id += "|" + contextId;
            }
        }

        return new SerializableTypeId( id );
    }

    // ReSharper disable once MemberCanBeInternal

    public static SerializableTypeId GetSerializableTypeId( this IType type, bool includeGenericContext = false, bool bypassSymbols = false )
    {
        var id = SyntaxGenerationContext.Contextless.SyntaxGenerator.TypeSyntax( type, bypassSymbols ).ToString();

        if ( type.IsNullable == false && type.IsReferenceType != false )
        {
            id += '!';
        }

        id = SerializableTypeId.Prefix + id;

        if ( includeGenericContext )
        {
            var genericContext = TypeParameterDetector.GetTypeContext( type );

            if ( genericContext != null )
            {
                // If there is a reference to a type parameter, we must append its context.
                var contextId = genericContext.GetSerializableId().Id;
                id += "|" + contextId;
            }
        }

        return new SerializableTypeId( id );
    }
}