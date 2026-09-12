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
/// The facet of an enum that an advice introduced.
/// </summary>
/// <remarks>
/// <para>
/// The members come from the builder data, which is the only place that records the order in which the aspect added
/// them, and which answers in every compilation that knows the enum rather than only in one to which the
/// transformations that register them have been applied.
/// </para>
/// </remarks>
internal sealed class IntroducedEnumFacet : EnumFacet
{
    private readonly IntroducedNamedType _type;

    public IntroducedEnumFacet( IntroducedNamedType type ) : base( type )
    {
        this._type = type;
    }

    [Memo]
    public override IReadOnlyList<IField> Members
        => this._type.MapDeclarationList( ((EnumBuilderData) this._type.NamedTypeBuilderData).Members );
}
