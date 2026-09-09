// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests of <see cref="IRecordFacet"/>, the facet that names the members that the compiler synthesizes for a record.
/// See issue #1997 and the design document <c>Metalama.Framework/docs/future/type-facets.md</c>.
/// </summary>
public sealed class RecordFacetTests : UnitTestClass
{
    private const string _code = """
                                 record PositionalRecordClass( int Id, string Name );

                                 record NonPositionalRecordClass
                                 {
                                     public int Id { get; init; }
                                 }

                                 record struct PositionalRecordStruct( int Id, string Name );

                                 record struct NonPositionalRecordStruct
                                 {
                                     public int Id { get; init; }
                                 }

                                 readonly record struct ReadOnlyRecordStruct( int Id );

                                 sealed record SealedPositionalRecordClass( int Id );

                                 class OrdinaryClass;
                                 struct OrdinaryStruct;
                                 interface IInterface;
                                 enum Enum { Value }
                                 delegate void Handler();
                                 """;

    /// <summary>
    /// Verifies the acceptance criterion that, for a positional record class, every member of the facet is present
    /// and resolves to the member that the compiler synthesized.
    /// </summary>
    [Fact]
    public void PositionalRecordClassHasEveryMemberOfTheFacet()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var type = compilation.Types.OfName( "PositionalRecordClass" ).Single();

        Assert.True( type.IsRecord );

        var facet = type.Facets.Record;

        Assert.NotNull( facet );
        Assert.Equal( TypeFacetKind.Record, facet.FacetKind );
        Assert.Same( type, facet.Type );

        var equalityContract = facet.EqualityContractProperty;

        Assert.NotNull( equalityContract );
        Assert.Equal( "EqualityContract", equalityContract.Name );
        Assert.Equal( "Type", ((INamedType) equalityContract.Type).Name );
        Assert.True( equalityContract.IsImplicitlyDeclared );

        var printMembers = facet.PrintMembersMethod;

        Assert.Equal( "PrintMembers", printMembers.Name );
        Assert.Equal( "Boolean", ((INamedType) printMembers.ReturnType).Name );
        Assert.Equal( "StringBuilder", ((INamedType) printMembers.Parameters.Single().Type).Name );
        Assert.True( printMembers.IsImplicitlyDeclared );

        var clone = facet.CloneMethod;

        Assert.NotNull( clone );
        Assert.Equal( "<Clone>$", clone.Name );
        Assert.Empty( clone.Parameters );
        Assert.True( clone.IsImplicitlyDeclared );

        var copyConstructor = facet.CopyConstructor;

        Assert.NotNull( copyConstructor );
        Assert.Same( type, copyConstructor.Parameters.Single().Type );
        Assert.Equal( Accessibility.Protected, copyConstructor.Accessibility );
        Assert.True( copyConstructor.IsImplicitlyDeclared );

        var deconstruct = facet.DeconstructMethod;

        Assert.NotNull( deconstruct );
        Assert.Equal( "Deconstruct", deconstruct.Name );
        Assert.Equal( new[] { "Id", "Name" }, deconstruct.Parameters.SelectAsArray( p => p.Name ) );
        Assert.All( deconstruct.Parameters, p => Assert.Equal( RefKind.Out, p.RefKind ) );
        Assert.True( deconstruct.IsImplicitlyDeclared );

        Assert.Equal( new[] { "Id", "Name" }, facet.PositionalProperties.SelectAsArray( p => p.Name ) );
        Assert.Equal( SpecialType.Int32, facet.PositionalProperties[0].Type.SpecialType );
        Assert.Equal( SpecialType.String, facet.PositionalProperties[1].Type.SpecialType );

        // The record facet is the only facet of the type.
        var facetCount = type.Facets.Count;

        Assert.Equal( 1, facetCount );
        Assert.Same( facet, Assert.Single( type.Facets ) );
    }

    /// <summary>
    /// Verifies that the members of the facet are the members of the type, and not copies of them built beside the
    /// collections of the type.
    /// </summary>
    [Fact]
    public void MembersOfTheFacetAreTheMembersOfTheType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var type = compilation.Types.OfName( "PositionalRecordClass" ).Single();
        var facet = type.Facets.Record;

        Assert.NotNull( facet );

        Assert.Equal( type.Properties.OfName( "Id" ).Single(), facet.PositionalProperties[0] );
        Assert.Equal( type.Properties.OfName( "Name" ).Single(), facet.PositionalProperties[1] );
        Assert.Equal( type.Methods.OfName( "Deconstruct" ).Single(), facet.DeconstructMethod );
        Assert.Same( type, facet.PrintMembersMethod.DeclaringType );
        Assert.Same( type, facet.CopyConstructor!.DeclaringType );
    }

    /// <summary>
    /// Verifies the acceptance criterion that, for a record struct, the equality contract, the clone method and the
    /// copy constructor are absent, and the rest of the facet is present.
    /// </summary>
    [Theory]
    [InlineData( "PositionalRecordStruct" )]
    [InlineData( "ReadOnlyRecordStruct" )]
    public void RecordStructHasNoEqualityContractNoCloneMethodAndNoCopyConstructor( string typeName )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var type = compilation.Types.OfName( typeName ).Single();

        Assert.True( type.IsRecord );

        var facet = type.Facets.Record;

        Assert.NotNull( facet );
        Assert.Null( facet.EqualityContractProperty );
        Assert.Null( facet.CloneMethod );
        Assert.Null( facet.CopyConstructor );

        Assert.Equal( "PrintMembers", facet.PrintMembersMethod.Name );
        Assert.NotNull( facet.DeconstructMethod );
        Assert.Equal( "Id", facet.PositionalProperties[0].Name );
    }

    /// <summary>
    /// Verifies the acceptance criterion that, for a record that is not positional, the <c>Deconstruct</c> method is
    /// absent and the list of positional properties is empty. The other members of the facet are present, because
    /// the compiler synthesizes them for every record of that kind.
    /// </summary>
    [Theory]
    [InlineData( "NonPositionalRecordClass", true )]
    [InlineData( "NonPositionalRecordStruct", false )]
    public void NonPositionalRecordHasNoDeconstructMethodAndNoPositionalProperty( string typeName, bool isRecordClass )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var type = compilation.Types.OfName( typeName ).Single();

        Assert.True( type.IsRecord );

        var facet = type.Facets.Record;

        Assert.NotNull( facet );
        Assert.Null( facet.DeconstructMethod );
        Assert.Empty( facet.PositionalProperties );

        Assert.Equal( "PrintMembers", facet.PrintMembersMethod.Name );
        Assert.Equal( isRecordClass, facet.EqualityContractProperty != null );
        Assert.Equal( isRecordClass, facet.CloneMethod != null );
        Assert.Equal( isRecordClass, facet.CopyConstructor != null );
    }

    /// <summary>
    /// Verifies that the facet names the copy constructor of a sealed record class, whose accessibility is private
    /// because the record cannot be inherited.
    /// </summary>
    [Fact]
    public void CopyConstructorOfSealedRecordClassIsPrivate()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var facet = compilation.Types.OfName( "SealedPositionalRecordClass" ).Single().Facets.Record;

        Assert.NotNull( facet );
        Assert.NotNull( facet.CopyConstructor );
        Assert.Equal( Accessibility.Private, facet.CopyConstructor.Accessibility );
    }

    /// <summary>
    /// Verifies the acceptance criterion that the facet is absent for a type that is not a record.
    /// </summary>
    [Fact]
    public void TypeThatIsNotARecordHasNoRecordFacet()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        foreach ( var typeName in new[] { "OrdinaryClass", "OrdinaryStruct", "IInterface", "Enum", "Handler" } )
        {
            var type = compilation.Types.OfName( typeName ).Single();

            Assert.False( type.IsRecord, $"{typeName} should not be a record." );
            Assert.Null( type.Facets.Record );
        }
    }

    /// <summary>
    /// Verifies that a record read from a referenced assembly reports the facet, so that the facet is resolved from
    /// the members of the type and not from the syntax of a record declaration.
    /// </summary>
    [Fact]
    public void RecordFromReferencedAssemblyHasTheFacet()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilation(
            "class C { PositionalRecordClass F = null!; }",
            dependentCode: _code );

        var type = (INamedType) compilation.Types.OfName( "C" ).Single().Fields.OfName( "F" ).Single().Type;

        Assert.True( type.IsRecord );

        var facet = type.Facets.Record;

        Assert.NotNull( facet );
        Assert.NotNull( facet.EqualityContractProperty );
        Assert.Equal( "PrintMembers", facet.PrintMembersMethod.Name );
        Assert.NotNull( facet.CloneMethod );
        Assert.NotNull( facet.CopyConstructor );
        Assert.NotNull( facet.DeconstructMethod );
        Assert.Equal( new[] { "Id", "Name" }, facet.PositionalProperties.SelectAsArray( p => p.Name ) );
    }

    /// <summary>
    /// Verifies that the facet of a constructed generic record names the members of the constructed type, so that
    /// the types that they carry are substituted.
    /// </summary>
    [Fact]
    public void FacetOfConstructedGenericRecordReportsTheSubstitutedMembers()
    {
        const string code = """
                            record Box<T>( T Value );

                            class Holder
                            {
                                public Box<string> ConstructedRecord = null!;
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );

        var constructedRecord = (INamedType) compilation.Types.OfName( "Holder" )
            .Single()
            .Fields.OfName( "ConstructedRecord" )
            .Single()
            .Type;

        var facet = constructedRecord.Facets.Record;

        Assert.NotNull( facet );
        Assert.Equal( SpecialType.String, facet.PositionalProperties.Single().Type.SpecialType );
        Assert.Equal( SpecialType.String, facet.DeconstructMethod!.Parameters.Single().Type.SpecialType );
        Assert.Same( constructedRecord, facet.CopyConstructor!.Parameters.Single().Type );

        // The generic definition reports the members that are not substituted.
        var definition = constructedRecord.Definition;

        Assert.Equal( definition.TypeParameters[0], definition.Facets.Record!.PositionalProperties.Single().Type );
    }
}
