// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Text;
using CodeTypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.SerializableIds;

[PublicAPI]
public static class SerializableTypeIdGenerator
{
    // A reflection Type is a long-lived, process-wide object, and its id is a pure function of it (the includeGenericContext
    // and bypassSymbols parameters of the Type overload are meaningless for a reflection type and do not affect the result),
    // so the id can be cached weakly by Type. This mirrors the caches in ReflectionHelper for symbols.
    private static readonly WeakCache<Type, SerializableTypeId> _reflectionTypeIdCache = new( isStaticCache: true );

    /// <summary>
    /// Determines whether a type reference carries information about the nullable context it was written in, which
    /// a reference type and a type parameter do and no other type does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A value type is <see cref="NullableAnnotation.NotAnnotated"/> in an unannotated context as well as in an
    /// annotated one, because it can never be oblivious: obliviousness is a statement about something that could be
    /// null. Its annotation therefore says nothing about the context and has to be ignored. A reference type is
    /// <see cref="NullableAnnotation.None"/> in an unannotated context and annotated in an annotated one, so it settles
    /// the question.
    /// </para>
    /// <para>
    /// A type parameter counts as well. The generic context appended to the identifier after a <c>|</c> resolves the
    /// parameter as its declaration declares it, which is oblivious, whereas a use of the parameter in an annotated
    /// context is not: without the marker a use of <c>T</c> in an annotated context resolved back to the declaration
    /// and lost the annotation. The constraint of the parameter is not what is being recorded, only the context of the
    /// reference.
    /// </para>
    /// </remarks>
    private static bool IsNullabilityInformative( ITypeSymbol symbol )
        => symbol.Kind == SymbolKind.TypeParameter || symbol.IsReferenceType;

    /// <summary>
    /// Determines whether the nullability marker is appended to the identifier of a type, which happens when the type
    /// was written in an annotated nullable context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The meaning of the marker, and the format of an identifier as a whole, are documented on
    /// <see cref="SerializableTypeId"/>. In short, <c>?</c> states that one type reference is nullable and the marker states
    /// that the whole reference was written in an annotated context, so that every type reference without a <c>?</c> is
    /// non-nullable rather than oblivious. The two are not alternatives and can both appear.
    /// </para>
    /// <para>
    /// A reference belongs to a single nullable context, that of the place it was written, so one bit describes all of
    /// its type references. The context is not recorded on a symbol and has to be recovered from the annotations, which the
    /// whole tree of the type is searched for: any informative type reference that is not
    /// <see cref="NullableAnnotation.None"/> proves the context was annotated. Reading the outermost type reference alone was
    /// not enough, because it is uninformative whenever it is a value type, as in
    /// <c>KeyValuePair&lt;string, string&gt;</c> or a tuple, and because an annotated outermost type says the type reference
    /// is nullable rather than that the context was unannotated, as in <c>List&lt;string&gt;?</c>. In each of those the
    /// marker was omitted and the reference types nested in the type came back oblivious.
    /// </para>
    /// <para>
    /// A type with no informative type reference at all, such as <c>KeyValuePair&lt;int, int&gt;</c>, needs no marker,
    /// nothing in it being able to be oblivious.
    /// </para>
    /// <para>
    /// The overloads of <c>GetSerializableTypeId</c> have to produce the same string for the same type, because
    /// <c>CompileTimeType</c> equality and the cache of <c>CompileTimeTypeFactory</c> key on it, so the same search is
    /// expressed over a symbol, over an <see cref="Code.IType"/> and over a reflection type.
    /// </para>
    /// </remarks>
    private static bool IsWrittenInAnnotatedContext( ITypeSymbol symbol )
    {
        // Only NotAnnotated proves the context, not any annotation other than None: writing 'string?' in an unannotated
        // context is a warning rather than an error, and Roslyn annotates it all the same, so Annotated proves nothing.
        // A reference type or a type parameter is never NotAnnotated in an unannotated context.
        if ( IsNullabilityInformative( symbol ) && symbol.NullableAnnotation == NullableAnnotation.NotAnnotated )
        {
            return true;
        }

        switch ( symbol.Kind )
        {
            case SymbolKind.ArrayType when symbol is IArrayTypeSymbol arrayType:
                return IsWrittenInAnnotatedContext( arrayType.ElementType );

            case SymbolKind.PointerType when symbol is IPointerTypeSymbol pointerType:
                return IsWrittenInAnnotatedContext( pointerType.PointedAtType );

            case SymbolKind.NamedType or SymbolKind.ErrorType when symbol is INamedTypeSymbol namedType:
                foreach ( var typeArgument in namedType.TypeArguments )
                {
                    if ( IsWrittenInAnnotatedContext( typeArgument ) )
                    {
                        return true;
                    }
                }

                return false;

            default:
                return false;
        }
    }

    public static SerializableTypeId GetSerializableTypeId( this ITypeSymbol symbol, bool includeGenericContext = false )
    {
        var id = SyntaxGenerationContext.Contextless.SyntaxGenerator.TypeSyntax( symbol ).ToString();

        if ( IsWrittenInAnnotatedContext( symbol ) )
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

    /// <summary>
    /// Answers what <see cref="IsWrittenInAnnotatedContext(ITypeSymbol)"/> answers, over the code model rather than
    /// over a symbol. A type reference is informative when it is a reference type or a type parameter, and it proves the
    /// context annotated when its nullability is known, <c>null</c> being how the code model reports obliviousness.
    /// </summary>
    private static bool IsWrittenInAnnotatedContext( IType type )
    {
        // The nullability of a type parameter is read from its symbol rather than from IsNullable, which answers
        // whether the constraint of the parameter allows null and not what the annotation of this reference is. The two
        // differ in both directions: a 'class' or 'notnull' constrained parameter reports IsNullable false where its
        // declaration is oblivious, and a use of an unconstrained parameter in an annotated context reports IsNullable
        // null where the reference is annotated. Either disagreement makes this overload produce a different identifier
        // from the one the overload over a symbol produces for the same type.
        if ( type.TypeKind == CodeTypeKind.TypeParameter )
        {
            return type is ISymbolBasedCompilationElement { Symbol: ITypeSymbol typeSymbol }
                   && typeSymbol.NullableAnnotation == NullableAnnotation.NotAnnotated;
        }

        // See the overload over a symbol: only a known non-nullable type reference proves the context, a nullable one being
        // written the same way in either.
        if ( type.IsReferenceType != false && type.IsNullable == false )
        {
            return true;
        }

        switch ( type.TypeKind )
        {
            case CodeTypeKind.Array when type is IArrayType arrayType:
                return IsWrittenInAnnotatedContext( arrayType.ElementType );

            case CodeTypeKind.Pointer when type is IPointerType pointerType:
                return IsWrittenInAnnotatedContext( pointerType.PointedAtType );

            case CodeTypeKind.Class or CodeTypeKind.Struct or CodeTypeKind.Interface or CodeTypeKind.Delegate or CodeTypeKind.Enum
                or CodeTypeKind.Error or CodeTypeKind.Tuple
                when type is INamedType namedType:
                foreach ( var typeArgument in namedType.TypeArguments )
                {
                    if ( IsWrittenInAnnotatedContext( typeArgument ) )
                    {
                        return true;
                    }
                }

                return false;

            default:
                return false;
        }
    }

    // ReSharper disable once MemberCanBeInternal

    public static SerializableTypeId GetSerializableTypeId( this IType type, bool includeGenericContext = false, bool bypassSymbols = false )
    {
        var id = SyntaxGenerationContext.Contextless.SyntaxGenerator.TypeSyntax( type, bypassSymbols ).ToString();

        if ( IsWrittenInAnnotatedContext( type ) )
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
        // Nullable<T> written as 'T?', with a trailing '!' when the type was written in an annotated context.
        //
        // No marker is appended here. A reflection type carries no nullable annotation and denotes a typeof expression,
        // which cannot name a nullable reference type at all, so it is null-oblivious and the unmarked form is what
        // describes it. Appending the marker for every reference type, as this did, made the identifier of a reflection
        // type claim an annotated context that a reflection type cannot be in.
        //
        // The code model deliberately answers differently: Factory.GetTypeByReflectionType returns a non-nullable type
        // rather than an oblivious one, because Metalama was written when the annotated context was already the norm
        // and that is the more useful default. The two therefore describe different types on purpose, and a caller that
        // wants to compare their identifiers removes the annotation first.
        var stringBuilder = new StringBuilder();
        stringBuilder.Append( SerializableTypeId.Prefix );
        AppendType( type );

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

}