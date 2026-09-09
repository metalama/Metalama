// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Utilities;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Types;

/// <summary>
/// Represents the structure of an enum: its underlying integral type, its members, and whether it carries
/// <see cref="System.FlagsAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// The facet is reached through <see cref="ITypeFacetCollection.Enum"/>, as in
/// <c>type.Facets.Enum?.UnderlyingType</c>. It is <c>null</c> for a type that <see cref="INamedType.IsEnum"/>
/// reports as not being an enum.
/// </para>
/// <para>
/// The members of an enum are exposed as <see cref="IField"/>. An enum member is a field, so no interface of its own
/// is declared for it.
/// </para>
/// </remarks>
/// <seealso cref="INamedType.IsEnum"/>
/// <seealso cref="INamedType.Facets"/>
[CompileTime]
[InternalImplement]
public interface IEnumFacet : ITypeFacet
{
    /// <summary>
    /// Gets the underlying integral type of the enum, which is <c>int</c> unless the declaration names another one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property has a single meaning. <see cref="INamedType.UnderlyingType"/> returns the same type for an
    /// enum, but it returns other things for other kinds of type, so the caller has to know the kind of the type to
    /// know what it returned.
    /// </para>
    /// </remarks>
    /// <seealso cref="INamedType.UnderlyingType"/>
    INamedType UnderlyingType { get; }

    /// <summary>
    /// Gets the members of the enum, in the order in which they are declared. The field that carries the underlying
    /// value, whose metadata name is <c>value__</c>, is not a member of the enum and is not in this list.
    /// </summary>
    IReadOnlyList<IField> Members { get; }

    /// <summary>
    /// Gets a value indicating whether the enum has the <see cref="System.FlagsAttribute"/> attribute.
    /// </summary>
    bool IsFlags { get; }
}
