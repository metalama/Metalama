// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SerializableIds;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

internal static class DurableRefFactory
{
    public static IDurableRef<T> FromDeclarationId<T>( SerializableDeclarationId id )
        where T : class, ICompilationElement
        => new DeclarationIdRef<T>( id );

    public static IDurableRef<T> FromTypeId<T>( SerializableTypeId id )
        where T : class, IType
        => new TypeIdRef<T>( id );

    /// <summary>
    /// Returns a durable reference to a given declaration or type, computing the identifier from the declaration or
    /// type itself instead of going through the non-durable <see cref="IFullRef{T}"/> that
    /// <see cref="IDeclaration.ToRef"/> returns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A declaration is identified by its <see cref="SerializableDeclarationId"/>, which is also what
    /// <see cref="IRef.ToDurable"/> uses, so both routes produce an equal reference.
    /// </para>
    /// <para>
    /// A type that is not a declaration, such as an array type or a pointer type, has no declaration identifier and is
    /// therefore identified by its <see cref="SerializableTypeId"/>.
    /// </para>
    /// </remarks>
    public static IDurableRef<T> FromDeclarationOrType<T>( ICompilationElement declarationOrType )
        where T : class, ICompilationElement
        => declarationOrType switch
        {
            IAttribute => throw new NotSupportedException( AttributeRef.CannotBeMadeDurableMessage ),
            IDeclaration declaration => new DeclarationIdRef<T>( declaration.GetSerializableId() ),
            IType type => new TypeIdRef<T>( type.GetSerializableTypeId() ),
            _ => throw new NotSupportedException(
                $"Cannot create a durable reference to a '{declarationOrType.DeclarationKind}' because it is neither a declaration nor a type." )
        };
}