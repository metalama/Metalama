// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Facets;

/// <summary>
/// Implementation of <see cref="IEnumFacet"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class stores the type only. <see cref="Members"/> is resolved by the derived class, from the symbol of a
/// type read from source and from the builder data of an introduced one, so constructing the facet of a type whose
/// structure is never read costs one allocation and no resolution.
/// </para>
/// </remarks>
internal abstract class EnumFacet : IEnumFacet
{
    protected EnumFacet( INamedType type )
    {
        this.Type = type;
    }

    public TypeFacetKind FacetKind => TypeFacetKind.Enum;

    public INamedType Type { get; }

    public INamedType UnderlyingType => this.Type.UnderlyingType;

    public abstract IReadOnlyList<IField> Members { get; }

    [Memo]
    public bool IsFlags => this.GetIsFlagsCore();

    private bool GetIsFlagsCore()
    {
        // The attribute type is matched by its name first, because that comparison is the cheapest one, and by its
        // namespace afterwards. Matching by typeof(FlagsAttribute) would resolve the reflection type through the
        // compilation, which costs more. The attributes of an introduced type are reported the same way as those of
        // a type read from source, so this member needs no implementation per source.
        foreach ( var attribute in this.Type.Attributes )
        {
            if ( attribute.Type.Name == nameof(FlagsAttribute) && attribute.Type.ContainingNamespace.FullName == "System" )
            {
                return true;
            }
        }

        return false;
    }
}
