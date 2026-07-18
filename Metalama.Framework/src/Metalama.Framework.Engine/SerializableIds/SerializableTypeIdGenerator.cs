// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Text;

namespace Metalama.Framework.Engine.SerializableIds;

[PublicAPI]
public static class SerializableTypeIdGenerator
{
    // A reflection Type is a long-lived, process-wide object, and its id is a pure function of it (the includeGenericContext
    // and bypassSymbols parameters of the Type overload are meaningless for a reflection type and do not affect the result),
    // so the id can be cached weakly by Type. This mirrors the caches in ReflectionHelper for symbols.
    private static readonly WeakCache<Type, SerializableTypeId> _reflectionTypeIdCache = new( isStaticCache: true );

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

    public static SerializableTypeId GetSerializableTypeId( this TypeSyntax typeSyntax )
    {
        return new SerializableTypeId( SerializableTypeId.Prefix + typeSyntax );
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

    public static SerializableTypeId GetSerializableTypeId( this Type type, bool includeGenericContext = false, bool bypassSymbols = false )
        => _reflectionTypeIdCache.GetOrAdd( type, static t => GetSerializableTypeIdCore( t ) );

    private static SerializableTypeId GetSerializableTypeIdCore( Type type )
    {
        // The id must be byte-identical to the one the ITypeSymbol/IType overloads produce (via the syntax generator),
        // because CompileTimeType equality and the CompileTimeTypeFactory cache key on this string. That form is C# type
        // syntax: 'global::'-qualified names, '<...>' generics whose arguments are separated by ',' with no space,
        // Nullable<T> written as 'T?', with a trailing '!' for a non-nullable reference type.
        var stringBuilder = new StringBuilder();
        stringBuilder.Append( SerializableTypeId.Prefix );
        AppendType( type );

        // The nullability annotation is appended once, for the outermost type only. A reference type is non-nullable
        // oblivious here (there is no annotation on a reflection Type), which the symbol side renders as '!'.
        if ( IsNonNullableReferenceType( type ) )
        {
            stringBuilder.Append( '!' );
        }

        return new SerializableTypeId( stringBuilder.ToString() );

        void AppendType( Type t )
        {
            var nullableUnderlyingType = Nullable.GetUnderlyingType( t );

            if ( nullableUnderlyingType != null )
            {
                // C# renders Nullable<T> as 'T?', not as 'System.Nullable<T>'.
                AppendType( nullableUnderlyingType );
                stringBuilder.Append( '?' );
            }
            else if ( t.IsArray )
            {
                AppendArrayType( t );
            }
            else if ( t.IsPointer )
            {
                AppendPointerType( t );
            }

#if NETCOREAPP
            else if ( t.IsFunctionPointer )
            {
                throw new NotImplementedException();
            }
#endif
            else if ( t.IsByRef )
            {
                AppendByRefType( t );
            }
            else if ( t.IsGenericParameter )
            {
                // A type parameter is written by its name alone (e.g. 'T', 'TKey'): no 'global::', no namespace.
                stringBuilder.Append( t.Name );
            }
            else
            {
                AppendNamedType( t );
            }
        }

        void AppendNamedType( Type namedType )
        {
            if ( namedType.IsNested )
            {
                // The declaring type already carries the namespace (and the 'global::' prefix) -- reflection reports the
                // same Namespace on a nested type as on its declaring type, so appending it again would yield
                // 'Ns.OuterNs.Inner'. In a type id (C# syntax) a nested type is separated from its declaring type by '.',
                // not by the reflection '+'.
                AppendNamedType( namedType.DeclaringType.AssertNotNull() );
                stringBuilder.Append( '.' );
            }
            else
            {
                stringBuilder.Append( "global::" );

                if ( !string.IsNullOrEmpty( namedType.Namespace ) )
                {
                    stringBuilder.Append( namedType.Namespace );
                    stringBuilder.Append( '.' );
                }
            }

            stringBuilder.Append( TypeNameHelper.StripArity( namedType.Name ) );

            if ( namedType.IsGenericType )
            {
                // GetGenericArguments returns the type parameters of an open definition and the arguments of a
                // constructed type; in both cases each is appended (a parameter renders as its name, e.g. 'List<T>' /
                // 'Dictionary<TKey,TValue>'), separated by ',' with no space, as the syntax generator produces.
                var genericArguments = namedType.GetGenericArguments();

                stringBuilder.Append( '<' );

                for ( var i = 0; i < genericArguments.Length; i++ )
                {
                    if ( i > 0 )
                    {
                        stringBuilder.Append( ',' );
                    }

                    AppendType( genericArguments[i] );
                }

                stringBuilder.Append( '>' );
            }
        }

        void AppendArrayType( Type arrayType )
        {
            AppendType( arrayType.GetElementType().AssertNotNull() );
            stringBuilder.Append( '[' );

            if ( arrayType.GetArrayRank() > 1 )
            {
                stringBuilder.Append( ',', arrayType.GetArrayRank() - 1 );
            }

            stringBuilder.Append( ']' );
        }

        void AppendPointerType( Type pointerType )
        {
            AppendType( pointerType.GetElementType().AssertNotNull() );
            stringBuilder.Append( '*' );
        }

        void AppendByRefType( Type byRefType )
        {
            AppendType( byRefType.GetElementType().AssertNotNull() );
            stringBuilder.Append( '&' );
        }
    }

    // A reference type is anything that is neither a value type (which includes Nullable<T>), a pointer, nor a by-ref
    // type. This mirrors the 'IsNullable == false && IsReferenceType != false' test the IType overload applies.
    private static bool IsNonNullableReferenceType( Type type ) => !type.IsValueType && !type.IsPointer && !type.IsByRef;
}