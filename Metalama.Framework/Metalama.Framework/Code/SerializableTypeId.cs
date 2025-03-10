// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code;

/// <summary>
/// Encapsulates a string that uniquely identifies a type within a compilation (except in the situation where the compilation
/// contains several assemblies providing types of the same name) and that is safe to persist in a file.
/// </summary>
[CompileTime]
public readonly struct SerializableTypeId : IEquatable<SerializableTypeId>
{
    internal const string LegacyPrefix = "typeof";
    internal const string Prefix = "Y:"; // T: is used for named types.

    internal static bool IsTypeId( string id ) => id.StartsWith( Prefix, StringComparison.Ordinal ) || id.StartsWith( LegacyPrefix, StringComparison.Ordinal );

    public string Id { get; }

    // Intentionally public because this is used in the Workspace project where we need to pass the id as a string.
    public SerializableTypeId( string id )
    {
        if ( !IsTypeId( id ) )
        {
            throw new ArgumentException( $"Invalid type id: '{id}'." );
        }

        this.Id = id;
    }

    public bool Equals( SerializableTypeId other ) => string.Equals( this.Id, other.Id, StringComparison.Ordinal );

    public override bool Equals( object? obj ) => obj is SerializableTypeId other && this.Equals( other );

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode( this.Id );

    public static bool operator ==( SerializableTypeId left, SerializableTypeId right ) => left.Equals( right );

    public static bool operator !=( SerializableTypeId left, SerializableTypeId right ) => !left.Equals( right );

    public IType Resolve( ICompilation compilation ) => this.Resolve( compilation, null );

    public IType Resolve( ICompilation compilation, IReadOnlyDictionary<string, IType>? genericArguments )
        => ((ICompilationInternal) compilation).Factory.GetTypeFromId( this, genericArguments );

    public override string ToString() => this.Id;
}