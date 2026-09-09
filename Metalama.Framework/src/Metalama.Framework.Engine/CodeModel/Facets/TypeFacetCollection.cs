// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Facets;

/// <summary>
/// Implementation of <see cref="ITypeFacetCollection"/>.
/// </summary>
/// <remarks>
/// <para>
/// A type that has no facet returns <see cref="Empty"/>, so the case of the ordinary class, struct or interface
/// allocates nothing. Code that walks the compilation reads <see cref="INamedType.Facets"/> on every type, so that
/// case is the one that has to be free.
/// </para>
/// <para>
/// Each facet of a type that has one is constructed on first read and memoized, so a type whose facets are never
/// read allocates the collection only.
/// </para>
/// </remarks>
internal sealed class TypeFacetCollection : ITypeFacetCollection
{
    /// <summary>
    /// Gets the collection returned by every type that has no facet.
    /// </summary>
    public static ITypeFacetCollection Empty { get; } = new EmptyTypeFacetCollection();

    private readonly INamedType _type;

    public TypeFacetCollection( INamedType type )
    {
        this._type = type;
    }

    /// <summary>
    /// Creates the collection of facets of a type, or returns <see cref="Empty"/> when the type has no facet.
    /// </summary>
    public static ITypeFacetCollection Create( INamedType type ) => type.IsDelegate || type.IsUnion ? new TypeFacetCollection( type ) : Empty;

    [Memo]
    public IDelegateFacet? Delegate => this._type.IsDelegate ? new DelegateFacet( this._type ) : null;

    [Memo]
    public IUnionFacet? Union => this._type.IsUnion ? new UnionFacet( this._type ) : null;

    public int Count => (this.Delegate == null ? 0 : 1) + (this.Union == null ? 0 : 1);

    public IEnumerator<ITypeFacet> GetEnumerator()
    {
        // The facets are yielded in the order of the members of TypeFacetKind, which is the order that the
        // documentation of ITypeFacetCollection states.
        if ( this.Delegate != null )
        {
            yield return this.Delegate;
        }

        if ( this.Union != null )
        {
            yield return this.Union;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private sealed class EmptyTypeFacetCollection : ITypeFacetCollection
    {
        public IDelegateFacet? Delegate => null;

        public IUnionFacet? Union => null;

        public int Count => 0;

        public IEnumerator<ITypeFacet> GetEnumerator() => Enumerable.Empty<ITypeFacet>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
