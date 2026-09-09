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
    /// The list is never empty for a well-formed union, because the language requires a union to have at least one
    /// case.
    /// </remarks>
    IReadOnlyList<IUnionCase> Cases { get; }

    /// <summary>
    /// Gets the <c>Value</c> property, which holds the value of the case that the union currently carries. The
    /// compiler synthesizes it for a union declaration, and the language requires the attribute form to declare it.
    /// </summary>
    IProperty ValueProperty { get; }
}
