// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using IMethodSymbol = Microsoft.CodeAnalysis.IMethodSymbol;
using RefKind = Metalama.Framework.Code.RefKind;
using SpecialType = Metalama.Framework.Code.SpecialType;

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

    public RecordFacet( INamedType type )
    {
        this.Type = type;
    }

    public TypeFacetKind FacetKind => TypeFacetKind.Record;

    public INamedType Type { get; }

    [Memo]
    public IProperty? EqualityContractProperty => this.Type.Properties.OfName( _equalityContractPropertyName ).SingleOrDefault();

    [Memo]
    public IMethod PrintMembersMethod
        => this.Type.Methods.OfName( _printMembersMethodName )
            .Single( m => m is { IsStatic: false, TypeParameters.Count: 0, Parameters: [_] } );

    [Memo]
    public IMethod? CloneMethod => this.GetCloneMethod();

    [Memo]
    public IConstructor? CopyConstructor
        => this.Type.Constructors.SingleOrDefault(
            c => c is { IsStatic: false, IsPrimary: false, Parameters: [var parameter] }
                 && parameter.Type.Equals( this.Type, TypeComparison.Default ) );

    [Memo]
    public IMethod? DeconstructMethod => this.GetDeconstructMethod();

    [Memo]
    public IReadOnlyList<IProperty> PositionalProperties => this.GetPositionalProperties();

    private IMethod? GetCloneMethod()
    {
        // The name of the clone method is not a C# identifier, so INamedType.Methods, which represents what can be
        // written in C#, does not contain it, and the facet resolves it from the symbol. The symbol of the type is
        // not available when the type is a generic instance whose members require mapping, and the clone method is
        // then not reported.
        if ( this.Type.GetSymbol() is not { } typeSymbol )
        {
            return null;
        }

        var cloneMethodSymbol = typeSymbol.GetMembers( _cloneMethodMetadataName ).OfType<IMethodSymbol>().FirstOrDefault();

        return cloneMethodSymbol == null ? null : this.Type.GetCompilationModel().Factory.GetMethod( cloneMethodSymbol );
    }

    private IMethod? GetDeconstructMethod()
    {
        // A record is positional when it declares a parameter list, which is what gives it a primary constructor.
        // The Deconstruct method is then the method whose out parameters are the positional parameters.
        if ( this.Type.PrimaryConstructor is not { } primaryConstructor )
        {
            return null;
        }

        return this.Type.Methods.OfName( _deconstructMethodName )
            .SingleOrDefault(
                m => m is { IsStatic: false, TypeParameters.Count: 0 }
                     && m.ReturnType.SpecialType == SpecialType.Void
                     && m.Parameters.Count == primaryConstructor.Parameters.Count
                     && m.Parameters.All( p => p.RefKind == RefKind.Out ) );
    }

    private IReadOnlyList<IProperty> GetPositionalProperties()
    {
        if ( this.Type.PrimaryConstructor is not { } primaryConstructor || primaryConstructor.Parameters.Count == 0 )
        {
            return [];
        }

        var properties = new List<IProperty>( primaryConstructor.Parameters.Count );

        foreach ( var parameter in primaryConstructor.Parameters )
        {
            // A positional parameter declares no property when the record, or one of its base records, declares a
            // member of that name itself. Such a parameter contributes no element.
            var property = this.Type.Properties.OfName( parameter.Name ).SingleOrDefault();

            if ( property != null )
            {
                properties.Add( property );
            }
        }

        return properties;
    }
}
