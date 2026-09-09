// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Utilities;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Collections;

/// <summary>
/// Read-only collection of the facets of a named type, reached through <see cref="INamedType.Facets"/>.
/// </summary>
/// <remarks>
/// <para>
/// The collection contains exactly the typed properties of this interface that are not <c>null</c>, and
/// <see cref="IReadOnlyCollection{T}.Count"/> is their number. Reading a facet through its typed property is
/// therefore equivalent to searching the collection for that kind, and it is the intended way to reach a facet.
/// </para>
/// <para>
/// The collection is empty for a type that has no facet, which is the case of most named types. That case allocates
/// nothing, because every such type returns one shared instance.
/// </para>
/// </remarks>
/// <seealso cref="INamedType.Facets"/>
/// <seealso cref="ITypeFacet"/>
[CompileTime]
[InternalImplement]
public interface ITypeFacetCollection : IReadOnlyCollection<ITypeFacet>
{
    /// <summary>
    /// Gets the facet of the delegate, or <c>null</c> when the type is not a delegate.
    /// </summary>
    IDelegateFacet? Delegate { get; }

    /// <summary>
    /// Gets the facet of the union, or <c>null</c> when the type is not a union.
    /// </summary>
    /// <seealso cref="INamedType.IsUnion"/>
    IUnionFacet? Union { get; }
}
