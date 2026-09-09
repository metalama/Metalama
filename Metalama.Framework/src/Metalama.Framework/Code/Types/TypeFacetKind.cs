// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.Types;

/// <summary>
/// Kinds of <see cref="ITypeFacet"/>.
/// </summary>
/// <seealso cref="ITypeFacet.FacetKind"/>
[CompileTime]
public enum TypeFacetKind
{
    /// <summary>
    /// The type has no facet. No instance of <see cref="ITypeFacet"/> reports this value.
    /// </summary>
    None = 0,

    /// <summary>
    /// The facet of a delegate (<see cref="IDelegateFacet"/>).
    /// </summary>
    Delegate,

    /// <summary>
    /// The facet of a union (<see cref="IUnionFacet"/>).
    /// </summary>
    Union,

    /// <summary>
    /// The facet of a record (<see cref="IRecordFacet"/>).
    /// </summary>
    Record
}
