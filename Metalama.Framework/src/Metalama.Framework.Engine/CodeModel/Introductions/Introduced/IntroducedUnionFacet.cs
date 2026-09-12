// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Facets;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.Utilities;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

/// <summary>
/// The facet of a union that an advice introduced.
/// </summary>
/// <remarks>
/// <para>
/// The cases and the <c>Value</c> property come from the builder data rather than from the members of the type,
/// because those collections are empty in a compilation to which the transformations that register the members have
/// not been applied. An aspect that reads the facet of a union it has just introduced reads it in exactly such a
/// compilation.
/// </para>
/// </remarks>
internal sealed class IntroducedUnionFacet : UnionFacet
{
    private readonly IntroducedNamedType _type;

    public IntroducedUnionFacet( IntroducedNamedType type ) : base( type )
    {
        this._type = type;
    }

    /// <summary>
    /// Always <see cref="UnionKind.Declaration"/>, because <c>IntroduceUnion</c> produces a union written with the
    /// <c>union</c> keyword and no other form.
    /// </summary>
    public override UnionKind UnionKind => UnionKind.Declaration;

    /// <summary>
    /// Gets the cases of the union, one per case constructor, in the order in which the aspect added them. The
    /// deduplication that a union read from source needs is not needed here, because
    /// <see cref="Metalama.Framework.Code.DeclarationBuilders.IUnionBuilder.AddCase(Metalama.Framework.Code.IType)"/>
    /// refuses a case type that is already a case.
    /// </summary>
    [Memo]
    public override IReadOnlyList<IUnionCase> Cases => this.GetCasesFromBuilderData();

    [Memo]
    public override IProperty? ValueProperty => this._type.MapDeclaration( this.BuilderData.ValueProperty );

    private UnionBuilderData BuilderData => (UnionBuilderData) this._type.NamedTypeBuilderData;

    private IReadOnlyList<IUnionCase> GetCasesFromBuilderData()
    {
        var caseConstructors = this.BuilderData.CaseConstructors;
        var cases = new IUnionCase[caseConstructors.Length];

        for ( var i = 0; i < cases.Length; i++ )
        {
            var constructor = this._type.MapDeclaration( caseConstructors[i] );

            cases[i] = new UnionCase( constructor.Parameters[0].Type, i, constructor );
        }

        return cases;
    }
}
