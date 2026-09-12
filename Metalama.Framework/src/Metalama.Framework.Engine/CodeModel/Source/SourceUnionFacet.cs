// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Facets;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Source;

/// <summary>
/// The facet of a union read from source or from a referenced assembly.
/// </summary>
internal sealed class SourceUnionFacet : UnionFacet
{
    public SourceUnionFacet( INamedType type ) : base( type ) { }

    /// <summary>
    /// Gets the authoring form of the union.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The declaration form is recognized from the syntax of the declaration, because the compiled form of a union
    /// is the same for the two forms: both carry the union attribute. A union that has no declaring syntax is read
    /// from a referenced assembly and is therefore reported as the attribute form, which is the form that its
    /// compiled shape has.
    /// </para>
    /// </remarks>
    [Memo]
    public override UnionKind UnionKind => this.GetUnionKindCore();

    [Memo]
    public override IReadOnlyList<IUnionCase> Cases => this.GetCasesFromMembers();

    [Memo]
    public override IProperty? ValueProperty => this.GetValuePropertyFromMembers();

    private UnionKind GetUnionKindCore()
    {
        var declaringSyntaxReferences = this.Type.Definition.GetSymbol()?.DeclaringSyntaxReferences ?? default;

        foreach ( var declaringSyntaxReference in declaringSyntaxReferences )
        {
            if ( declaringSyntaxReference.GetSyntax().SyntaxKind.IsUnionDeclaration )
            {
                return UnionKind.Declaration;
            }
        }

        return UnionKind.Attribute;
    }
}
