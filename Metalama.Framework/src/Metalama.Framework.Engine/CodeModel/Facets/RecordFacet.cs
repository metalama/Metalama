// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using IMethodSymbol = Microsoft.CodeAnalysis.IMethodSymbol;
using RefKind = Metalama.Framework.Code.RefKind;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Facets;

/// <summary>
/// Implementation of <see cref="IRecordFacet"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class stores the type only. Every other member is resolved on first read and memoized, so constructing the
/// facet of a type whose synthesized members are never read costs one allocation and no resolution.
/// </para>
/// <para>
/// A member is identified by its signature and not by the fact that the compiler declared it implicitly, because a
/// record read from a referenced assembly reports every member as explicitly declared.
/// </para>
/// <para>
/// The signature must be matched completely, because a record is allowed to declare a member whose name is the name
/// of a synthesized member and whose signature is not its signature. A record may declare an overload of
/// <c>PrintMembers</c> or of <c>Deconstruct</c>, and a record struct may declare a property named
/// <c>EqualityContract</c> or a constructor whose single parameter is the record struct itself.
/// </para>
/// </remarks>
internal sealed class RecordFacet : IRecordFacet
{
    /// <summary>
    /// The identifiers of the members that the compiler synthesizes for a record. This class is the single site of
    /// the code model that resolves them: every other consumer reaches them through the facet.
    /// </summary>
    private const string _equalityContractPropertyName = "EqualityContract";

    private const string _printMembersMethodName = "PrintMembers";
    private const string _cloneMethodMetadataName = "<Clone>$";
    private const string _deconstructMethodName = "Deconstruct";

    /// <summary>
    /// The full name of the type of the single parameter of the <c>PrintMembers</c> method.
    /// </summary>
    private const string _stringBuilderTypeFullName = "System.Text.StringBuilder";

    public RecordFacet( INamedType type )
    {
        this.Type = type;
    }

    public TypeFacetKind FacetKind => TypeFacetKind.Record;

    public INamedType Type { get; }

    /// <summary>
    /// Gets a value indicating whether the type is a record class, as opposed to a record struct. The compiler
    /// synthesizes the equality contract, the clone method and the copy constructor for a record class only, so the
    /// three properties that report them return <c>null</c> for a record struct.
    /// </summary>
    private bool IsRecordClass => this.Type.TypeKind == TypeKind.Class;

    /// <summary>
    /// Gets the type as an introduced type, or <c>null</c> when it is read from source.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The members that the compiler synthesizes for an introduced record come from the builder data, which is the
    /// only place that records them and which answers in every compilation that knows the record rather than only
    /// in one to which the transformations that register them have been applied. Two of them cannot be recovered
    /// from the code model at all: the name of the clone method is not a C# identifier, so
    /// <see cref="INamedType.Methods"/> never contains it, and a positional property is told from an ordinary one
    /// by its declaring syntax, which an introduced property does not have.
    /// </para>
    /// </remarks>
    private IntroducedNamedType? IntroducedType => this.Type as IntroducedNamedType;

    [Memo]
    public IProperty? EqualityContractProperty => this.GetEqualityContractProperty();

    private IProperty? GetEqualityContractProperty()
    {
        if ( !this.IsRecordClass )
        {
            return null;
        }

        return this.IntroducedType?.EqualityContractProperty
               ?? this.Type.Properties.OfName( _equalityContractPropertyName ).SingleOrDefault();
    }

    [Memo]
    public IMethod PrintMembersMethod
        => this.IntroducedType?.PrintMembersMethod ?? this.Type.Methods.OfName( _printMembersMethodName ).Single( IsPrintMembersMethod );

    [Memo]
    public IMethod? CloneMethod => this.IsRecordClass ? this.GetCloneMethod() : null;

    [Memo]
    public IConstructor? CopyConstructor
        => this.IsRecordClass
            ? this.Type.Constructors.SingleOrDefault(
                c => c is { IsStatic: false, IsPrimary: false, Parameters: [var parameter] }
                     && parameter.Type.Equals( this.Type, TypeComparison.Default ) )
            : null;

    [Memo]
    public IMethod? DeconstructMethod => this.GetDeconstructMethod();

    [Memo]
    public IReadOnlyList<IProperty> PositionalProperties => this.GetPositionalProperties();

    /// <summary>
    /// Determines whether a method has the signature of the <c>PrintMembers</c> method that the compiler
    /// synthesizes, which is <c>bool PrintMembers( StringBuilder builder )</c>.
    /// </summary>
    private static bool IsPrintMembersMethod( IMethod method )
        => method is
           {
               IsStatic: false,
               TypeParameters.Count: 0,
               Parameters: [{ RefKind: RefKind.None, Type: INamedType { FullName: _stringBuilderTypeFullName } }]
           }
           && method.ReturnType.SpecialType == SpecialType.Boolean;

    private IMethod? GetCloneMethod()
    {
        if ( this.IntroducedType is { } introducedType )
        {
            return introducedType.CloneMethod;
        }

        // The name of the clone method is not a C# identifier, so INamedType.Methods, which represents what can be
        // written in C#, does not contain it, and the facet resolves it from the symbol of the type. That symbol is
        // the generic definition when the type is a generic instance whose type arguments cannot be expressed as
        // symbols, so the generic context of the type maps the resolved method back to the type instance.
        if ( this.Type is not ISymbolBasedCompilationElement { Symbol: INamedTypeSymbol typeSymbol } symbolBasedType )
        {
            return null;
        }

        var cloneMethodSymbol = typeSymbol.GetMembers( _cloneMethodMetadataName ).OfType<IMethodSymbol>().FirstOrDefault();

        if ( cloneMethodSymbol == null )
        {
            return null;
        }

        return this.Type.GetCompilationModel()
            .Factory.GetMethod( cloneMethodSymbol, (GenericContext) symbolBasedType.GenericContextForSymbolMapping );
    }

    /// <summary>
    /// Gets the primary constructor of the type, whose parameters are the positional parameters of the record, or
    /// <c>null</c> when the record is not positional.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The constructor is taken from <see cref="INamedType.Constructors"/> and not from
    /// <see cref="INamedType.PrimaryConstructor"/>, because the parameters of the latter are those of the generic
    /// definition when the type is a constructed generic type, so their types are the type parameters and not the
    /// type arguments.
    /// </para>
    /// </remarks>
    private IConstructor? GetPrimaryConstructor()
        => this.IntroducedType != null ? this.Type.PrimaryConstructor : this.Type.Constructors.SingleOrDefault( c => c.IsPrimary );

    private IMethod? GetDeconstructMethod()
    {
        if ( this.IntroducedType is { } introducedType )
        {
            return introducedType.DeconstructMethod;
        }

        // A record is positional when it declares a parameter list, which is what gives it a primary constructor.
        // The Deconstruct method is then the method whose out parameters are the positional parameters.
        if ( this.GetPrimaryConstructor() is not { } primaryConstructor )
        {
            return null;
        }

        return this.Type.Methods.OfName( _deconstructMethodName ).SingleOrDefault( m => IsDeconstructMethod( m, primaryConstructor ) );
    }

    /// <summary>
    /// Determines whether a method has the signature of the <c>Deconstruct</c> method that the compiler synthesizes
    /// for a positional record, which is a <c>void</c> method whose parameters are the parameters of the primary
    /// constructor, each of them an <c>out</c> parameter.
    /// </summary>
    private static bool IsDeconstructMethod( IMethod method, IConstructor primaryConstructor )
    {
        if ( method is not { IsStatic: false, TypeParameters.Count: 0 }
             || method.ReturnType.SpecialType != SpecialType.Void
             || method.Parameters.Count != primaryConstructor.Parameters.Count )
        {
            return false;
        }

        for ( var index = 0; index < method.Parameters.Count; index++ )
        {
            var parameter = method.Parameters[index];

            if ( parameter.RefKind != RefKind.Out
                 || !parameter.Type.Equals( primaryConstructor.Parameters[index].Type, TypeComparison.Default ) )
            {
                return false;
            }
        }

        return true;
    }

    private IReadOnlyList<IProperty> GetPositionalProperties()
    {
        if ( this.IntroducedType is { } introducedType )
        {
            return introducedType.PositionalProperties;
        }

        if ( this.GetPrimaryConstructor() is not { } primaryConstructor || primaryConstructor.Parameters.Count == 0 )
        {
            return [];
        }

        var properties = new List<IProperty>( primaryConstructor.Parameters.Count );

        foreach ( var parameter in primaryConstructor.Parameters )
        {
            // A positional parameter declares no property when the record, or one of its base records, declares a
            // member of that name itself. Such a parameter contributes no element. The property that a positional
            // parameter declares has that parameter as its declaration, which is how the two cases are told apart.
            // A record has a parameter list in source only, so the declaration is always available here.
            var property = this.Type.Properties.OfName( parameter.Name ).SingleOrDefault();

            if ( property?.GetPrimaryDeclarationSyntax() is ParameterSyntax )
            {
                properties.Add( property );
            }
        }

        return properties;
    }
}
