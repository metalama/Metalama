// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.CodeModel.Source;
using System.Collections.Generic;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Facets;

/// <summary>
/// Implementation of <see cref="IRecordFacet"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class stores the type only. Every member below is resolved by the derived class, from the members of a type
/// read from source and from the builder data of an introduced one, so constructing the facet of a type whose
/// synthesized members are never read costs one allocation and no resolution.
/// </para>
/// </remarks>
internal abstract class RecordFacet : IRecordFacet
{
    protected RecordFacet( INamedType type )
    {
        this.Type = type;
    }

    /// <summary>
    /// Creates the facet of a record, which is the implementation for the source of that type.
    /// </summary>
    public static RecordFacet Create( INamedType type )
        => type is IntroducedNamedType introducedType ? new IntroducedRecordFacet( introducedType ) : new SourceRecordFacet( type );

    public TypeFacetKind FacetKind => TypeFacetKind.Record;

    public INamedType Type { get; }

    /// <summary>
    /// Gets a value indicating whether the type is a record class, as opposed to a record struct. The compiler
    /// synthesizes the equality contract, the clone method and the copy constructor for a record class only, so the
    /// three properties that report them return <c>null</c> for a record struct.
    /// </summary>
    protected bool IsRecordClass => this.Type.TypeKind == TypeKind.Class;

    public abstract IProperty? EqualityContractProperty { get; }

    public abstract IMethod PrintMembersMethod { get; }

    public abstract IMethod? CloneMethod { get; }

    public abstract IConstructor? CopyConstructor { get; }

    public abstract IMethod? DeconstructMethod { get; }

    public abstract IReadOnlyList<IProperty> PositionalProperties { get; }
}
