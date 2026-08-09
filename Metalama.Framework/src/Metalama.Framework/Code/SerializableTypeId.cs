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
/// The two nullability markers describe different things and are not alternatives. <c>?</c> belongs to one type reference and means
/// that this type reference is nullable, exactly as in C#. The <c>!</c> at the end belongs to the whole identifier and means that
/// the type was written in an annotated nullable context, so that every type reference without a <c>?</c> is non-nullable rather
/// than oblivious. A reference belongs to a single nullable context, that of the place it was written, which is why one
/// marker suffices for all of its type references.
/// </para>
/// <list type="table">
/// <listheader><term>Identifier</term><description>Type</description></listheader>
/// <item><term><c>Y:global::System.String!</c></term><description>the non-nullable <c>string</c>.</description></item>
/// <item><term><c>Y:global::System.String?!</c></term><description>the nullable <c>string?</c>.</description></item>
/// <item>
/// <term><c>Y:global::System.String</c></term>
/// <description>the <c>string</c> of a context that is oblivious to nullability.</description>
/// </item>
/// <item>
/// <term><c>Y:global::System.Collections.Generic.List&lt;global::System.String?&gt;!</c></term>
/// <description>a non-nullable <c>List&lt;string?&gt;</c>, the list itself carrying no <c>?</c> and its argument carrying
/// one.</description>
/// </item>
/// </list>
/// <para>
/// An identifier that carries no marker therefore resolves to a type every type reference of which is oblivious to nullability, and
/// not to a non-nullable one.
/// </para>
/// <para>
/// The marker is written whenever any type reference of the type proves the context annotated, and applies to every type the
/// identifier names when it is resolved. Only a reference type and a type parameter prove anything: a value type is not
/// annotated in an unannotated context any more than in an annotated one, because it can never be oblivious. A type with no
/// reference type and no type parameter anywhere in it, such as <c>KeyValuePair&lt;int, int&gt;</c>, therefore carries no
/// marker and needs none.
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