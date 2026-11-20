// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Facets;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using System.Collections.Generic;
#if ROSLYN_5_12_0_OR_GREATER
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Microsoft.CodeAnalysis;
using System.Linq;
#endif

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

    /// <summary>
    /// Gets the cases of the union.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In the latest Roslyn variant, the case types and their order are read from
    /// <c>Microsoft.CodeAnalysis.ITypeSymbol.UnionCaseTypes</c>, and the creation member of each case is the first
    /// creation member whose parameter has the type of the case. The lower Roslyn variant does not declare that
    /// member, so it derives the cases from the members of the type. See section 6 of
    /// <c>Metalama.Framework/docs/2027.0/DECISIONS.md</c>.
    /// </para>
    /// </remarks>
    [Memo]
#if ROSLYN_5_12_0_OR_GREATER
    public override IReadOnlyList<IUnionCase> Cases => this.GetCasesFromCompiler() ?? this.GetCasesFromMembers();
#else
    public override IReadOnlyList<IUnionCase> Cases => this.GetCasesFromMembers();
#endif

    [Memo]
    public override IProperty? ValueProperty => this.GetValuePropertyFromMembers();

#if ROSLYN_5_12_0_OR_GREATER

    /// <summary>
    /// Reads the cases of the union from <c>Microsoft.CodeAnalysis.ITypeSymbol.UnionCaseTypes</c>, or returns
    /// <c>null</c> when the type is not backed by a symbol, when a case type has no creation member, or when a
    /// creation member has a parameter type that is not a case type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The compiler reports the case types without a nullable annotation. The annotated type remains available as
    /// the type of the parameter of <see cref="IUnionCase.CreationMember"/>.
    /// </para>
    /// <para>
    /// The symbol of the type is the generic definition when the type is a generic instance whose type arguments
    /// cannot be expressed as symbols, so the generic context of the type maps each case type to the type instance.
    /// </para>
    /// <para>
    /// A case type that has no creation member occurs only in code that the compiler rejects. The method then
    /// returns <c>null</c>, and the caller derives the cases from the members of the type.
    /// </para>
    /// <para>
    /// The symbol describes the original compilation and does not include the members that an aspect introduces.
    /// When an aspect introduces a creation member whose parameter type is not a case type of the compiler, the
    /// method also returns <c>null</c>, so that the derivation reports the case that the introduced member adds.
    /// </para>
    /// </remarks>
    private IReadOnlyList<IUnionCase>? GetCasesFromCompiler()
    {
        if ( this.Type is not ISymbolBasedCompilationElement { Symbol: INamedTypeSymbol typeSymbol } symbolBasedType )
        {
            return null;
        }

        var factory = this.Type.GetCompilationModel().Factory;
        var genericContext = (GenericContext) symbolBasedType.GenericContextForSymbolMapping;
        var comparer = this.Type.Compilation.Comparers.Default;
        var creationMembers = this.GetCreationMembers().ToList();

        var cases = new List<IUnionCase>( typeSymbol.UnionCaseTypes.Length );

        foreach ( var caseTypeSymbol in typeSymbol.UnionCaseTypes )
        {
            var caseType = factory.GetIType( caseTypeSymbol, genericContext, defaultNullability: null );
            var creationMember = creationMembers.FirstOrDefault( member => comparer.Equals( member.Parameters[0].Type, caseType ) );

            if ( creationMember == null )
            {
                return null;
            }

            cases.Add( new UnionCase( caseType, cases.Count, creationMember ) );
        }

        // An introduced creation member can add a case that the symbol does not report.
        if ( !creationMembers.All( member => cases.Any( unionCase => comparer.Equals( member.Parameters[0].Type, unionCase.Type ) ) ) )
        {
            return null;
        }

        return cases;
    }

#endif

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
