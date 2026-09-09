// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Types;
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
/// </remarks>
internal sealed class TypeFacetCollection : ITypeFacetCollection
{
    /// <summary>
    /// The identifier of the method that carries the signature of a delegate. This class is the single site of the
    /// code model that resolves it: every other consumer reaches the method through the facet.
    /// </summary>
    private const string _invokeMethodName = nameof(System.Action.Invoke);

    /// <summary>
    /// Gets the collection returned by every type that has no facet.
    /// </summary>
    public static ITypeFacetCollection Empty { get; } = new TypeFacetCollection( null );

    private TypeFacetCollection( IDelegateFacet? delegateFacet )
    {
        this.Delegate = delegateFacet;
    }

    /// <summary>
    /// Creates the collection of facets of a type, or returns <see cref="Empty"/> when the type has no facet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>Invoke</c> method of a delegate is resolved here, because the collection cannot report whether the type
    /// has the facet without it. A type that is not a delegate resolves nothing.
    /// </para>
    /// </remarks>
    public static ITypeFacetCollection Create( INamedType type )
    {
        if ( !type.IsDelegate )
        {
            return Empty;
        }

        // A delegate declared in source or read from well-formed metadata declares exactly one Invoke method. The
        // method is absent from a type read from malformed metadata, and the type then has no facet, which is the
        // condition that the consumers of the facet test.
        var invokeMethod = type.Methods.OfName( _invokeMethodName ).SingleOrDefault();

        if ( invokeMethod == null )
        {
            return Empty;
        }

        return new TypeFacetCollection( new DelegateFacet( type, invokeMethod ) );
    }

    public IDelegateFacet? Delegate { get; }

    public int Count => this.Delegate == null ? 0 : 1;

    public IEnumerator<ITypeFacet> GetEnumerator()
    {
        if ( this.Delegate != null )
        {
            yield return this.Delegate;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
