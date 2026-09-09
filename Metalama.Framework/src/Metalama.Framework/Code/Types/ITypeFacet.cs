// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code.Types;

/// <summary>
/// Represents the structure that a named type has because of its kind, and that other named types do not have.
/// </summary>
/// <remarks>
/// <para>
/// A facet names the members that the compiler synthesizes for a kind of type, so that no consumer has to reach such
/// a member by its identifier. The facets of a type are reached through <see cref="INamedType.Facets"/>.
/// </para>
/// </remarks>
/// <seealso cref="INamedType.Facets"/>
/// <seealso cref="ITypeFacetCollection"/>
[CompileTime]
[InternalImplement]
public interface ITypeFacet
{
    /// <summary>
    /// Gets the kind of the facet.
    /// </summary>
    TypeFacetKind FacetKind { get; }

    /// <summary>
    /// Gets the type to which the facet belongs.
    /// </summary>
    INamedType Type { get; }
}
