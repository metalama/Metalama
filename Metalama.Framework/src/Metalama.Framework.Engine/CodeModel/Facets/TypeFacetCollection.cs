// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.CodeModel.Source;
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
/// <para>
/// The derived class decides which implementation of each facet to construct, and <see cref="Create"/> is the
/// single site that reads the source of a type. No facet and no collection routes on it afterwards.
/// </para>
/// </remarks>
internal abstract class TypeFacetCollection : ITypeFacetCollection
{
    /// <summary>
    /// Gets the collection returned by every type that has no facet.
    /// </summary>
    public static ITypeFacetCollection Empty { get; } = new EmptyTypeFacetCollection();

    protected INamedType Type { get; }

    protected TypeFacetCollection( INamedType type )
    {
        this.Type = type;
    }

    /// <summary>
    /// Creates the collection of facets of a type, or returns <see cref="Empty"/> when the type has no facet.
    /// </summary>
    public static ITypeFacetCollection Create( INamedType type )
    {
        if ( !(type.IsDelegate || type.IsUnion || type.IsRecord || type.IsEnum) )
        {
            return Empty;
        }

        return type is IntroducedNamedType introducedType
            ? new IntroducedTypeFacetCollection( introducedType )
            : new SourceTypeFacetCollection( type );
    }

    protected abstract IDelegateFacet CreateDelegateFacet();

    protected abstract IUnionFacet CreateUnionFacet();

    protected abstract IRecordFacet CreateRecordFacet();

    protected abstract IEnumFacet CreateEnumFacet();

    [Memo]
    public IDelegateFacet? Delegate => this.Type.IsDelegate ? this.CreateDelegateFacet() : null;

    [Memo]
    public IUnionFacet? Union => this.Type.IsUnion ? this.CreateUnionFacet() : null;

    [Memo]
    public IRecordFacet? Record => this.Type.IsRecord ? this.CreateRecordFacet() : null;

    [Memo]
    public IEnumFacet? Enum => this.Type.IsEnum ? this.CreateEnumFacet() : null;

    // The count is answered from the discriminators of the type and not from the typed properties, so that counting
    // the facets of a type does not construct them.
    public int Count
        => (this.Type.IsDelegate ? 1 : 0) + (this.Type.IsUnion ? 1 : 0) + (this.Type.IsRecord ? 1 : 0) + (this.Type.IsEnum ? 1 : 0);

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

        if ( this.Record != null )
        {
            yield return this.Record;
        }

        if ( this.Enum != null )
        {
            yield return this.Enum;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private sealed class EmptyTypeFacetCollection : ITypeFacetCollection
    {
        public IDelegateFacet? Delegate => null;

        public IUnionFacet? Union => null;

        public IRecordFacet? Record => null;

        public IEnumFacet? Enum => null;

        public int Count => 0;

        public IEnumerator<ITypeFacet> GetEnumerator() => Enumerable.Empty<ITypeFacet>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
