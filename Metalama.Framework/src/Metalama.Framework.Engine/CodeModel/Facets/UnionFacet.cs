// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Accessibility = Metalama.Framework.Code.Accessibility;

namespace Metalama.Framework.Engine.CodeModel.Facets;

/// <summary>
/// Implementation of <see cref="IUnionFacet"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class stores the type only. Every other member is resolved on first read and memoized, so constructing the
/// facet of a type whose structure is never read costs one allocation and no resolution.
/// </para>
/// <para>
/// The structure is derived from the members of the type and not from
/// <c>Microsoft.CodeAnalysis.ITypeSymbol.UnionCaseTypes</c>, which the consumed Roslyn build does not publish. The
/// derivation follows the definition that Roslyn itself applies: the cases of a union are the first parameter types
/// of its creation members. Reading the Roslyn member instead becomes possible with issue #1936.
/// </para>
/// </remarks>
internal sealed class UnionFacet : IUnionFacet
{
    /// <summary>
    /// The identifier of the property that holds the value of the case that a union currently carries. The compiler
    /// synthesizes the property for a union declaration, and the language requires the attribute form to declare it.
    /// </summary>
    private const string _valuePropertyName = "Value";

    /// <summary>
    /// The identifier of the nested interface on which the attribute form of a union may declare its creation
    /// members, which the language proposal names a union member provider.
    /// </summary>
    private const string _memberProviderInterfaceName = "IUnionMembers";

    /// <summary>
    /// The identifier of the static creation methods of a union member provider.
    /// </summary>
    private const string _creationMethodName = "Create";

    public UnionFacet( INamedType type )
    {
        this.Type = type;
    }

    public TypeFacetKind FacetKind => TypeFacetKind.Union;

    public INamedType Type { get; }

    [Memo]
    public UnionKind UnionKind => GetUnionKind( this.Type );

    [Memo]
    public IReadOnlyList<IUnionCase> Cases => GetCases( this.Type );

    [Memo]
    public IProperty ValueProperty => this.Type.Properties.OfName( _valuePropertyName ).Single();

    private static UnionKind GetUnionKind( INamedType type )
    {
        // The authoring form is read from the syntax of the declaration, because the compiled form of a union is the
        // same for the two forms: both carry the union attribute. A union that has no declaring syntax therefore
        // comes from a referenced assembly and its form cannot be recovered.
        var declaringSyntaxReferences = type.Definition.GetSymbol()?.DeclaringSyntaxReferences ?? default;

        if ( declaringSyntaxReferences.IsDefaultOrEmpty )
        {
            return UnionKind.None;
        }

        foreach ( var declaringSyntaxReference in declaringSyntaxReferences )
        {
            if ( declaringSyntaxReference.GetSyntax().SyntaxKind.IsUnionDeclaration )
            {
                return UnionKind.Declaration;
            }
        }

        return UnionKind.Attribute;
    }

    private static IReadOnlyList<IUnionCase> GetCases( INamedType type )
    {
        var cases = new List<IUnionCase>();

        foreach ( var creationMember in GetCreationMembers( type ) )
        {
            cases.Add( new UnionCase( creationMember.Parameters[0].Type, cases.Count, creationMember ) );
        }

        return cases;
    }

    /// <summary>
    /// Enumerates the creation members of a union, which are the members that create a value of one of its cases.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A union that declares a union member provider interface creates the value of a case through a public static
    /// method named <c>Create</c>, and every other union creates it through a public constructor that takes one
    /// parameter. A union declaration always takes the second branch, because the language forbids a union
    /// declaration to declare a member provider interface.
    /// </para>
    /// <para>
    /// The methods are read from the union and not from the member provider interface, which declares them as static
    /// abstract members. The member of the union is the one that a consumer can invoke.
    /// </para>
    /// </remarks>
    private static IEnumerable<IMethodBase> GetCreationMembers( INamedType type )
    {
        if ( type.Types.OfName( _memberProviderInterfaceName ).Any() )
        {
            return type.Methods
                .OfName( _creationMethodName )
                .Where( method => method is { IsStatic: true, Accessibility: Accessibility.Public, Parameters.Count: 1 } );
        }

        return type.Constructors
            .Where( constructor => constructor is { IsStatic: false, Accessibility: Accessibility.Public, Parameters.Count: 1 } );
    }
}
