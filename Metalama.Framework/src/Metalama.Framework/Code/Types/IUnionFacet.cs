// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Utilities;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Types;

/// <summary>
/// Represents the structure of a union of C# 15: its authoring form, its cases and its <c>Value</c> property.
/// </summary>
/// <remarks>
/// <para>
/// The facet is reached through <see cref="ITypeFacetCollection.Union"/>, as in <c>type.Facets.Union?.Cases</c>. It is
/// <c>null</c> for a type that <see cref="INamedType.IsUnion"/> reports as not being a union.
/// </para>
/// <para>
/// A union is not a kind of its own in the code model. The compiler reports a union declaration as a struct, and the
/// attribute form keeps the kind of the class or the struct that carries the attribute, so
/// <see cref="INamedType.IsUnion"/> is the property that tells a union from an ordinary type and this facet is the
/// structure behind it.
/// </para>
/// </remarks>
/// <seealso cref="INamedType.IsUnion"/>
/// <seealso cref="INamedType.Facets"/>
[CompileTime]
[InternalImplement]
public interface IUnionFacet : ITypeFacet
{
    /// <summary>
    /// Gets the authoring form of the union.
    /// </summary>
    UnionKind UnionKind { get; }

    /// <summary>
    /// Gets the cases of the union, in the order in which the creation members of the union declare them, which is the
    /// order of the union header for a union declaration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The list is never empty for a well-formed union, because the language requires a union to have at least one
    /// case.
    /// </para>
    /// <para>
    /// A type occurs once in the list, as it does in the case set of the compiler. Two creation members that take the
    /// same type therefore declare one case, whose <see cref="IUnionCase.CreationMember"/> is the first of the two.
    /// </para>
    /// </remarks>
    IReadOnlyList<IUnionCase> Cases { get; }

    /// <summary>
    /// Gets the <c>Value</c> property, which holds the value of the case that the union currently carries, or
    /// <c>null</c> when the union has no such property. The compiler synthesizes the property for a union
    /// declaration, and the language requires the attribute form to have one, so the value is <c>null</c> only for a
    /// union that the compiler reports as ill-formed, which the code model of a design-time session does see.
    /// </summary>
    /// <remarks>
    /// The property is the one that the compiler resolves, which is not necessarily declared by the union itself: the
    /// attribute form may inherit it from a base type, and a union that has a union member provider interface
    /// declares it on that interface.
    /// </remarks>
    IProperty? ValueProperty { get; }
}
