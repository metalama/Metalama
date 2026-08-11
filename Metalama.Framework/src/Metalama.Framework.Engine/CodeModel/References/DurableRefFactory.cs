// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Options;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// Builds durable references at the call sites that have no service provider, and therefore no
/// <see cref="IDurableRefFactory"/>, such as the deserialization of a reference or a reflection mock.
/// </summary>
/// <remarks>
/// These methods build an identifier-based reference whatever the scope of the project. That is the only possible
/// answer here: they start from an identifier rather than from a declaration, so there is nothing live to hold, and a
/// reference read from a serialized stream is by definition read in a compilation other than the one that wrote it.
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
    /// <paramref name="kind"/> is <see cref="DurableRefKind.Default"/>, which names no factory: the choice is then made
    /// by the execution scenario, in <c>AspectPipeline</c>.
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
