// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code;

/// <summary>
/// Encapsulates a string that uniquely identifies a declaration within a compilation (except in the situation where the compilation
/// contains several assemblies providing types of the same name) and that is safe to persist in a file.
/// </summary>
/// <seealso cref="SerializableTypeId"/>
[CompileTime]
public readonly struct SerializableDeclarationId : IEquatable<SerializableDeclarationId>
{
    // Intentionally public because of serialization.
    public string Id { get; }

    // Intentionally public because this is used in the Workspace project where we need to pass the id as a string.
    public SerializableDeclarationId( string id )
    {
        this.Id = id;
    }

    public bool Equals( SerializableDeclarationId other ) => string.Equals( this.Id, other.Id, StringComparison.Ordinal );

    public override bool Equals( object? obj ) => obj is SerializableDeclarationId other && this.Equals( other );

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode( this.Id );

    public static bool operator ==( SerializableDeclarationId left, SerializableDeclarationId right ) => left.Equals( right );

    public static bool operator !=( SerializableDeclarationId left, SerializableDeclarationId right ) => !left.Equals( right );

    public IDeclaration Resolve( ICompilation compilation ) => ((ICompilationInternal) compilation).Factory.GetDeclarationFromId( this );

    public override string ToString() => this.Id ?? "(null)";
}