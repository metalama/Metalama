// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Facets;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.Utilities;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

/// <summary>
/// The facet of a delegate that an advice introduced.
/// </summary>
/// <remarks>
/// <para>
/// The <c>Invoke</c> method comes from the builder data rather than from
/// <see cref="INamedType.Methods"/>, because that collection is empty in a compilation to which the transformation
/// that registers the method has not been applied. An aspect that types an event by a delegate it has just
/// introduced reads the facet in exactly such a compilation.
/// </para>
/// </remarks>
internal sealed class IntroducedDelegateFacet : DelegateFacet
{
    private readonly IntroducedNamedType _type;

    public IntroducedDelegateFacet( IntroducedNamedType type ) : base( type )
    {
        this._type = type;
    }

    [Memo]
    public override IMethod InvokeMethod => this._type.MapDeclaration( ((DelegateBuilderData) this._type.NamedTypeBuilderData).InvokeMethod );
}
