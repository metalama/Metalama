// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code;

/// <summary>
/// Encapsulates a string that uniquely identifies a declaration within a compilation and that is safe to persist in a file
/// or serialize across processes.
/// </summary>
/// <remarks>
/// <para>
/// This struct provides a lightweight, string-based identifier for declarations that can be serialized to disk or transmitted
/// across processes. Unlike <see cref="IRef{T}"/>, which maintains a reference to the declaration, this struct stores only
/// the string identifier.
/// </para>
/// <para>
/// To obtain a <see cref="SerializableDeclarationId"/>, call <see cref="IDeclaration.ToSerializableId"/> or
/// <see cref="IRef.ToSerializableId"/>. To resolve the identifier back to a declaration, call
/// <see cref="Resolve"/> or <see cref="IDeclarationFactory.GetDeclarationFromId"/>.
/// </para>
/// <para>
/// <b>Limitation:</b> The identifier may not be unique if the compilation contains several assemblies providing types
/// with the same fully qualified name.
/// </para>
/// </remarks>
/// <seealso cref="SerializableTypeId"/>
/// <seealso cref="IRef{T}"/>
/// <seealso cref="IDeclaration.ToSerializableId"/>
/// <seealso href="@aspect-serialization"/>
[CompileTime]
public readonly struct SerializableDeclarationId : IEquatable<SerializableDeclarationId>
{
    /// <summary>
    /// Gets the string identifier for this declaration.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializableDeclarationId"/> struct with the specified identifier string.
    /// </summary>
    /// <param name="id">The string identifier for the declaration.</param>
    public SerializableDeclarationId( string id )
    {
        this.Id = id;
    }

    /// <inheritdoc />
    public bool Equals( SerializableDeclarationId other ) => string.Equals( this.Id, other.Id, StringComparison.Ordinal );

    /// <inheritdoc />
    public override bool Equals( object? obj ) => obj is SerializableDeclarationId other && this.Equals( other );

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode( this.Id );

    /// <summary>
    /// Determines whether two <see cref="SerializableDeclarationId"/> instances are equal.
    /// </summary>
    public static bool operator ==( SerializableDeclarationId left, SerializableDeclarationId right ) => left.Equals( right );

    /// <summary>
    /// Determines whether two <see cref="SerializableDeclarationId"/> instances are not equal.
    /// </summary>
    public static bool operator !=( SerializableDeclarationId left, SerializableDeclarationId right ) => !left.Equals( right );

    /// <summary>
    /// Resolves this identifier to the corresponding <see cref="IDeclaration"/> in the specified compilation.
    /// </summary>
    /// <param name="compilation">The compilation in which to resolve the declaration.</param>
    /// <returns>The resolved declaration.</returns>
    public IDeclaration Resolve( ICompilation compilation ) => ((ICompilationInternal) compilation).Factory.GetDeclarationFromId( this );

    /// <inheritdoc />
    public override string ToString() => this.Id ?? "(null)";
}