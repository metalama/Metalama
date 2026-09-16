// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Facets;

namespace Metalama.Framework.Engine.CodeModel.Source;

/// <summary>
/// The facets of a type read from source or from a referenced assembly.
/// </summary>
internal sealed class SourceTypeFacetCollection : TypeFacetCollection
{
    private SourceTypeFacetCollection( SourceNamedType type ) : base( type ) { }

    /// <summary>
    /// Creates the collection of facets of a type, or returns <see cref="TypeFacetCollection.Empty"/> when the type has no facet.
    /// </summary>
    public static ITypeFacetCollection Create( SourceNamedType type )
    {
        if ( !(type.IsDelegate || type.IsUnion || type.IsRecord || type.IsEnum) )
        {
            return Empty;
        }

        return new SourceTypeFacetCollection( type );
    }

    protected override IDelegateFacet CreateDelegateFacet() => new SourceDelegateFacet( this.Type );

    protected override IUnionFacet CreateUnionFacet() => new SourceUnionFacet( this.Type );

    protected override IRecordFacet CreateRecordFacet() => new SourceRecordFacet( this.Type );

    protected override IEnumFacet CreateEnumFacet() => new SourceEnumFacet( this.Type );
}