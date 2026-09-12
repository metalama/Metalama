// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Facets;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

/// <summary>
/// The facets of a type that an advice introduced.
/// </summary>
internal sealed class IntroducedTypeFacetCollection : TypeFacetCollection
{
    private readonly IntroducedNamedType _type;

    public IntroducedTypeFacetCollection( IntroducedNamedType type ) : base( type )
    {
        this._type = type;
    }

    protected override IDelegateFacet CreateDelegateFacet() => new IntroducedDelegateFacet( this._type );

    protected override IUnionFacet CreateUnionFacet() => new IntroducedUnionFacet( this._type );

    protected override IRecordFacet CreateRecordFacet() => new IntroducedRecordFacet( this._type );

    protected override IEnumFacet CreateEnumFacet() => new IntroducedEnumFacet( this._type );
}
