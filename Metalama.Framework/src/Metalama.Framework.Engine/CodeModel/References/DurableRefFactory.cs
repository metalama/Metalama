// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Options;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// Creates durable references at the call sites that have no service provider, and therefore no
/// <see cref="IDurableRefFactory"/>. Examples are the deserialization of a reference and the reflection mocks.
/// </summary>
/// <remarks>
/// These methods always create an identifier-based reference, in every execution scenario. Their argument is an
/// identifier and not a declaration, so there is no reference to store. A reference read from a serialized stream is
/// also read in a compilation other than the one that wrote it.
/// </remarks>
internal static class DurableRefFactory
{
    public static IDurableRef<T> FromDeclarationId<T>( SerializableDeclarationId id )
        where T : class, ICompilationElement
        => new DeclarationIdRef<T>( id );

    public static IDurableRef<T> FromTypeId<T>( SerializableTypeId id )
        where T : class, IType
        => new TypeIdRef<T>( id );

    /// <inheritdoc cref="SerializableDurableRefFactory.FromDeclarationOrType{T}"/>
    public static IDurableRef<T> FromDeclarationOrType<T>( ICompilationElement declarationOrType )
        where T : class, ICompilationElement
        => SerializableDurableRefFactory.Instance.FromDeclarationOrType<T>( declarationOrType );

    /// <summary>
    /// Returns the factory that implements a given <see cref="DurableRefKind"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="kind"/> is <see cref="DurableRefKind.Default"/>. That value designates no implementation,
    /// because <c>AspectPipeline</c> then selects the implementation according to the execution scenario.
    /// </exception>
    public static IDurableRefFactory GetFactory( DurableRefKind kind )
        => kind switch
        {
            DurableRefKind.Live => LiveDurableRefFactory.Instance,
            DurableRefKind.Serializable => SerializableDurableRefFactory.Instance,
            DurableRefKind.SerializableWithoutCache => SerializableDurableRefFactory.InstanceWithoutResolutionCache,
            _ => throw new ArgumentOutOfRangeException( nameof(kind), kind, null )
        };
}
