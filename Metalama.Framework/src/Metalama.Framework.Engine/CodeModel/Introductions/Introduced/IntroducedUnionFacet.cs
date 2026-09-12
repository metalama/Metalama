// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Facets;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

/// <summary>
/// The facet of a union that an advice introduced.
/// </summary>
/// <remarks>
/// <para>
/// The cases and the <c>Value</c> property are derived from the members of the type, which the advice registers, so
/// the base class answers them for an introduced union as it does for one read from source. Only the authoring form
/// differs, because it is read from a declaration that an introduced type does not have.
/// </para>
/// </remarks>
internal sealed class IntroducedUnionFacet : UnionFacet
{
    public IntroducedUnionFacet( IntroducedNamedType type ) : base( type ) { }

    /// <summary>
    /// Always <see cref="UnionKind.Declaration"/>, because <c>IntroduceUnion</c> produces a union written with the
    /// <c>union</c> keyword and no other form.
    /// </summary>
    public override UnionKind UnionKind => UnionKind.Declaration;
}
