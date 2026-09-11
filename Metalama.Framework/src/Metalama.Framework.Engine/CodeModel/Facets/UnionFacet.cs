// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using System.Collections.Generic;
using System.Linq;

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
/// derivation reproduces the rules that the compiler applies: the member provider interface, the suitable creation
/// members, the deduplication of the case types and the lookup of the <c>Value</c> property in the hierarchy.
/// Reading the Roslyn member instead becomes possible with issue #1936.
/// </para>
/// </remarks>
internal sealed class UnionFacet : IUnionFacet
{
    /// <summary>
    /// The identifier of the property that holds the value of the case that a union currently carries. The compiler
    /// synthesizes the property for a union declaration, and the language requires the attribute form to declare it
    /// or to inherit it.
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
    public IReadOnlyList<IUnionCase> Cases => this.GetCases();

    [Memo]
    public IProperty? ValueProperty => this.GetValueProperty();

    /// <summary>
    /// Gets the union member provider interface of the union, or <c>null</c> when the union has none, in which case
    /// the creation members of the union are its constructors.
    /// </summary>
    [Memo]
    private INamedType? MemberProviderInterface => this.GetMemberProviderInterface();

    private static UnionKind GetUnionKind( INamedType type )
    {
        // An introduced union is always written with the union keyword, because IntroduceUnion produces that form
        // and no other. It has no symbol, so the reading below would report it as the attribute form, which is the
        // silent failure that section 5 of Metalama.Framework/docs/future/introducing-unions.md names.
        if ( type is IntroducedNamedType )
        {
            return UnionKind.Declaration;
        }

        // The declaration form is recognized from the syntax of the declaration, because the compiled form of a union
        // is the same for the two forms: both carry the union attribute. A union that has no declaring syntax is read
        // from a referenced assembly and is therefore reported as the attribute form, which is the form that its
        // compiled shape has.
        var declaringSyntaxReferences = type.Definition.GetSymbol()?.DeclaringSyntaxReferences ?? default;

        foreach ( var declaringSyntaxReference in declaringSyntaxReferences )
        {
            if ( declaringSyntaxReference.GetSyntax().SyntaxKind.IsUnionDeclaration )
            {
                return UnionKind.Declaration;
            }
        }

        return UnionKind.Attribute;
    }

    private IReadOnlyList<IUnionCase> GetCases()
    {
        var cases = new List<IUnionCase>();

        // The compiler collects the case types in a set, so a creation member whose parameter type is already a case
        // adds no case, and the index of a case is its position after that deduplication. Two creation members can
        // have the same parameter type without being the same member, as an overload that takes the parameter by
        // value and an overload that takes it by 'in' do.
        var caseTypes = new HashSet<IType>( this.Type.Compilation.Comparers.Default );

        foreach ( var creationMember in this.GetCreationMembers() )
        {
            var caseType = creationMember.Parameters[0].Type;

            if ( caseTypes.Add( caseType ) )
            {
                cases.Add( new UnionCase( caseType, cases.Count, creationMember ) );
            }
        }

        return cases;
    }

    /// <summary>
    /// Enumerates the creation members of the union, which are the members that create a value of one of its cases,
    /// in the order in which the compiler collects them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A union that has a union member provider interface creates the value of a case through a static method named
    /// <c>Create</c> of that interface or of an interface that the interface inherits, and every other union creates
    /// it through a public constructor that takes one parameter. A union declaration always takes the second branch,
    /// because the language forbids a union declaration to declare a member provider interface.
    /// </para>
    /// <para>
    /// The methods are read from the member provider interface and not from the union, because the interface is what
    /// the compiler reads. A union may declare a public static <c>Create</c> method that is not a creation member,
    /// and it may inherit a creation member from an interface that its own member provider interface extends.
    /// </para>
    /// </remarks>
    private IEnumerable<IMethodBase> GetCreationMembers()
    {
        var memberProviderInterface = this.MemberProviderInterface;

        if ( memberProviderInterface == null )
        {
            return this.Type.Constructors.Where( IsSuitableCreationConstructor );
        }

        // The interface itself is read first and the interfaces that it inherits follow, which is the order of the
        // compiler and therefore the order of the cases.
        return GetMemberProviderInterfaces( memberProviderInterface )
            .SelectMany( declaringInterface => declaringInterface.Methods.OfName( _creationMethodName ) )
            .Where( this.IsSuitableCreationMethod );
    }

    /// <summary>
    /// Enumerates the member provider interface of the union followed by the interfaces that it inherits, which is
    /// the order in which the compiler looks a member of the provider up.
    /// </summary>
    private static IEnumerable<INamedType> GetMemberProviderInterfaces( INamedType memberProviderInterface )
    {
        yield return memberProviderInterface;

        foreach ( var baseInterface in memberProviderInterface.AllImplementedInterfaces )
        {
            yield return baseInterface;
        }
    }

    /// <summary>
    /// Returns the union member provider interface of the union, or <c>null</c> when the union has none. The
    /// interface is a nongeneric public nested interface named <c>IUnionMembers</c> that the union implements. A
    /// nested type of that name that does not meet those conditions is not a member provider, and the union then has
    /// none at all, which is how the compiler reads it.
    /// </summary>
    private INamedType? GetMemberProviderInterface()
    {
        foreach ( var nestedType in this.Type.Types.OfName( _memberProviderInterfaceName ) )
        {
            if ( nestedType.TypeParameters.Count != 0 )
            {
                continue;
            }

            if ( nestedType is not { Accessibility: Accessibility.Public, TypeKind: TypeKind.Interface }
                 || !this.ImplementsInterface( nestedType ) )
            {
                return null;
            }

            return nestedType;
        }

        return null;
    }

    private bool ImplementsInterface( INamedType interfaceType )
    {
        var comparer = this.Type.Compilation.Comparers.Default;

        return this.Type.AllImplementedInterfaces.Any( implementedInterface => comparer.Equals( implementedInterface, interfaceType ) );
    }

    private bool IsSuitableCreationMethod( IMethod method )
        => method is
           {
               IsStatic: true,
               Accessibility: Accessibility.Public,
               MethodKind: MethodKind.Default,
               TypeParameters.Count: 0,
               ReturnParameter.RefKind: RefKind.None,
               Parameters.Count: 1
           }
           && IsSuitableCreationParameter( method.Parameters[0] )
           && this.Type.Compilation.Comparers.Default.Equals( method.ReturnType, this.Type );

    private static bool IsSuitableCreationConstructor( IConstructor constructor )
        => constructor is { IsStatic: false, Accessibility: Accessibility.Public, Parameters.Count: 1 }
           && IsSuitableCreationParameter( constructor.Parameters[0] );

    /// <summary>
    /// Returns a value indicating whether the single parameter of a candidate creation member is passed in a way that
    /// makes the member a creation member. The compiler admits a parameter passed by value or by <c>in</c> and no
    /// other, so a <c>ref</c>, a <c>ref readonly</c> or an <c>out</c> parameter does not declare a case.
    /// </summary>
    private static bool IsSuitableCreationParameter( IParameter parameter ) => parameter.RefKind is RefKind.None or RefKind.In;

    /// <summary>
    /// Returns the <c>Value</c> property of the union, or <c>null</c> when the union has none, which the compiler
    /// reports as an error. The property is looked up the way the compiler looks it up: in the member provider
    /// interface and the interfaces that it inherits when the union has one, and in the union and its base types
    /// otherwise.
    /// </summary>
    private IProperty? GetValueProperty()
    {
        var memberProviderInterface = this.MemberProviderInterface;

        if ( memberProviderInterface != null )
        {
            return GetMemberProviderInterfaces( memberProviderInterface )
                .Select( FindValueProperty )
                .FirstOrDefault( property => property != null );
        }

        for ( var declaringType = this.Type; declaringType != null; declaringType = declaringType.BaseType )
        {
            var valueProperty = FindValueProperty( declaringType );

            if ( valueProperty != null )
            {
                return valueProperty;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the property of <paramref name="declaringType"/> that has the signature that the compiler requires of
    /// the <c>Value</c> property of a union, which is an instance property of type <see cref="object"/> returned by
    /// value and having a public getter, or <c>null</c> when the type declares no such property.
    /// </summary>
    private static IProperty? FindValueProperty( INamedType declaringType )
        => declaringType.Properties
            .OfName( _valuePropertyName )
            .FirstOrDefault(
                property => property is
                {
                    IsStatic: false,
                    RefKind: RefKind.None,
                    GetMethod.Accessibility: Accessibility.Public,
                    Type.SpecialType: SpecialType.Object
                } );
}
