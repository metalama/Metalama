// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// Creates durable references at the call sites that have no service provider, and therefore no direct access to the
/// <see cref="IDurableRefFactory"/> of the project.
/// </summary>
internal static class DurableRefFactory
{
    /// <summary>
    /// Creates a durable reference from a <see cref="SerializableDeclarationId"/>.
    /// </summary>
    /// <remarks>
    /// The result is always identifier-based, in every execution scenario. The argument is an identifier and not a
    /// declaration, so there is no reference to store. This method is called by the deserialization of a reference,
    /// and a reference is read in a compilation other than the one that wrote it.
    /// </remarks>
    public static IDurableRef<T> FromDeclarationId<T>( SerializableDeclarationId id )
        where T : class, ICompilationElement
        => new DeclarationIdRef<T>( id );

    /// <inheritdoc cref="FromDeclarationId{T}"/>
    public static IDurableRef<T> FromTypeId<T>( SerializableTypeId id )
        where T : class, IType
        => new TypeIdRef<T>( id );

    /// <inheritdoc cref="SerializedDurableRefFactory.FromDeclarationOrType{T}"/>
    /// <remarks>
    /// The representation is the one selected for the project that declares <paramref name="declarationOrType"/>, which
    /// is reached through its compilation.
    /// </remarks>
    public static IDurableRef<T> FromDeclarationOrType<T>( ICompilationElement declarationOrType )
        where T : class, ICompilationElement
        => ((CompilationModel) declarationOrType.Compilation).RefFactory.DurableRefFactory
        .FromDeclarationOrType<T>( declarationOrType );
}
