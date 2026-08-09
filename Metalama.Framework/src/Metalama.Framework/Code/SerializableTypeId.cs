// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code;

/// <summary>
/// Encapsulates a string that uniquely identifies a type within a compilation (except in the situation where the compilation
/// contains several assemblies providing types of the same name) and that is safe to persist in a file.
/// </summary>
/// <remarks>
/// <para>
/// The identifier is composed of the <c>Y:</c> prefix, the type written in C# syntax, an optional <c>!</c> marker, and an
/// optional generic context introduced by <c>|</c>. The type is written with <c>global::</c>-qualified names, with the
/// arguments of a generic type separated by a comma and no space, and with <see cref="System.Nullable{T}"/> written as
/// <c>T?</c>.
/// </para>
/// <para>
/// The <c>!</c> marker means that the type is a non-nullable reference type, and it is appended for no other type. C# syntax
/// can state that a reference type is nullable, by writing <c>string?</c>, but it cannot distinguish a reference type that is
/// known to be non-nullable from one whose nullability is unknown, because both are written <c>string</c>. The marker records
/// that distinction, so that the three identifiers below denote three different types:
/// </para>
/// <list type="table">
/// <listheader><term>Identifier</term><description>Type</description></listheader>
/// <item><term><c>Y:global::System.String!</c></term><description>the non-nullable <c>string</c>.</description></item>
/// <item><term><c>Y:global::System.String?</c></term><description>the nullable <c>string?</c>.</description></item>
/// <item>
/// <term><c>Y:global::System.String</c></term>
/// <description>the <c>string</c> of a context that is oblivious to nullability.</description>
/// </item>
/// </list>
/// <para>
/// An identifier that carries no marker therefore resolves to a type that is oblivious to nullability, and not to a
/// non-nullable one.
/// </para>
/// <para>
/// A value type never carries the marker, because it is not a reference type and because a nullable value type is written
/// <c>T?</c> instead. A type parameter never carries it either, because its nullability comes from the declaration that the
/// generic context resolves, which the marker could only contradict: C# cannot state that a type parameter is non-nullable.
/// </para>
/// <para>
/// The marker is written once, after the outermost type, and applies to every name of the identifier when it is resolved. The
/// nullability of an argument of a generic type is written on the argument itself, as in
/// <c>Y:global::System.Collections.Generic.List&lt;global::System.String?&gt;!</c>.
/// </para>
/// </remarks>
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