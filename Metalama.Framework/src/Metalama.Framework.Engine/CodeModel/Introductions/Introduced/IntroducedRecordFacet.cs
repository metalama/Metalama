// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Facets;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.Utilities;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

/// <summary>
/// The facet of a record that an advice introduced.
/// </summary>
/// <remarks>
/// <para>
/// Every member comes from the builder data, which is the only place that records them and which answers in every
/// compilation that knows the record rather than only in one to which the transformations that register them have
/// been applied. Two of them cannot be recovered from the code model at all: the name of the clone method is not a
/// C# identifier, so <see cref="INamedType.Methods"/> never contains it, and a positional property is told from an
/// ordinary one by its declaring syntax, which an introduced property does not have.
/// </para>
/// </remarks>
internal sealed class IntroducedRecordFacet : RecordFacet
{
    private readonly IntroducedNamedType _type;

    public IntroducedRecordFacet( IntroducedNamedType type ) : base( type )
    {
        this._type = type;
    }

    private RecordBuilderData BuilderData => (RecordBuilderData) this._type.NamedTypeBuilderData;

    [Memo]
    public override IProperty? EqualityContractProperty => this._type.MapDeclaration( this.BuilderData.EqualityContractProperty );

    [Memo]
    public override IMethod PrintMembersMethod => this._type.MapDeclaration( this.BuilderData.PrintMembersMethod );

    [Memo]
    public override IMethod? CloneMethod => this._type.MapDeclaration( this.BuilderData.CloneMethod );

    [Memo]
    public override IConstructor? CopyConstructor => this._type.MapDeclaration( this.BuilderData.CopyConstructor );

    [Memo]
    public override IMethod? DeconstructMethod => this._type.MapDeclaration( this.BuilderData.DeconstructMethod );

    [Memo]
    public override IReadOnlyList<IProperty> PositionalProperties => this._type.MapDeclarationList( this.BuilderData.PositionalProperties );
}
