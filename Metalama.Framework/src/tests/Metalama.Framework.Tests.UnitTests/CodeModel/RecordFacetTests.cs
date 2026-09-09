// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
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
    /// <summary>
    /// Gets the code of the compilation under test.
    /// </summary>
    /// <remarks>
    /// The reference assemblies of .NET Framework do not define <c>IsExternalInit</c>, which the <c>init</c>
    /// accessor of a positional record requires, so the type is declared in the code itself.
    /// </remarks>
    private static string GetCode()
    {
        // ReSharper disable once ConvertToConstant.Local
        var code = """
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

                   record RecordWithUnrelatedOverloads( int Id )
                   {
                       public bool PrintMembers( int unused ) => false;

                       public void Deconstruct( out string other ) => other = "";
                   }

                   record RecordWithDeclaredPositionalProperty( int Id )
                   {
                       public int Id { get; init; } = Id;
                   }

                   record struct RecordStructWithOwnEqualityContractAndCopyConstructor( int Id )
                   {
                       public int EqualityContract => 0;

                       public RecordStructWithOwnEqualityContractAndCopyConstructor(
                           RecordStructWithOwnEqualityContractAndCopyConstructor other ) : this( other.Id ) { }
                   }

                   class OrdinaryClass;
                   struct OrdinaryStruct;
                   interface IInterface;
                   enum Enum { Value }
                   delegate void Handler();
                   """;

#if !NET5_0_OR_GREATER
        code += "namespace System.Runtime.CompilerServices { internal static class IsExternalInit {} }";
#endif

        return code;
    }

    /// <summary>
    /// Verifies the acceptance criterion that, for a positional record class, every member of the facet is present
    /// and resolves to the member that the compiler synthesized.
    /// </summary>
    [Fact]
    public void PositionalRecordClassHasEveryMemberOfTheFacet()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( GetCode() );

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
        Assert.True( copyConstructor.Parameters.Single().Type.Equals( type, TypeComparison.Default ) );
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
        var compilation = testContext.CreateCompilation( GetCode() );

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
        var compilation = testContext.CreateCompilation( GetCode() );

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
        var compilation = testContext.CreateCompilation( GetCode() );

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
        var compilation = testContext.CreateCompilation( GetCode() );

        var facet = compilation.Types.OfName( "SealedPositionalRecordClass" ).Single().Facets.Record;

        Assert.NotNull( facet );
        Assert.NotNull( facet.CopyConstructor );
        Assert.Equal( Accessibility.Private, facet.CopyConstructor.Accessibility );
    }

    /// <summary>
    /// Verifies that the facet names the synthesized members of a record that declares an overload of
    /// <c>PrintMembers</c> and an overload of <c>Deconstruct</c> beside them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The compiler accepts both overloads, because neither has the signature of the member that it synthesizes, so
    /// a facet that selected its members by name and by the number of parameters would find two candidates and
    /// would throw.
    /// </para>
    /// </remarks>
    [Fact]
    public void RecordWithUnrelatedOverloadsReportsTheSynthesizedMembers()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( GetCode() );

        var facet = compilation.Types.OfName( "RecordWithUnrelatedOverloads" ).Single().Facets.Record;

        Assert.NotNull( facet );

        var printMembers = facet.PrintMembersMethod;

        Assert.Equal( "StringBuilder", ((INamedType) printMembers.Parameters.Single().Type).Name );
        Assert.Equal( SpecialType.Boolean, printMembers.ReturnType.SpecialType );
        Assert.True( printMembers.IsImplicitlyDeclared );

        var deconstruct = facet.DeconstructMethod;

        Assert.NotNull( deconstruct );
        Assert.Equal( SpecialType.Int32, deconstruct.Parameters.Single().Type.SpecialType );
        Assert.True( deconstruct.IsImplicitlyDeclared );
    }

    /// <summary>
    /// Verifies that a positional parameter whose property the record declares itself contributes no element to
    /// <see cref="IRecordFacet.PositionalProperties"/>, because the compiler then synthesizes no property for that
    /// parameter.
    /// </summary>
    [Fact]
    public void PositionalParameterWhosePropertyIsDeclaredContributesNoElement()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( GetCode() );

        var type = compilation.Types.OfName( "RecordWithDeclaredPositionalProperty" ).Single();
        var facet = type.Facets.Record;

        Assert.NotNull( facet );
        Assert.Empty( facet.PositionalProperties );

        // The property is a member of the type, and the record remains positional.
        Assert.False( type.Properties.OfName( "Id" ).Single().IsImplicitlyDeclared );
        Assert.NotNull( facet.DeconstructMethod );
    }

    /// <summary>
    /// Verifies that a record struct that declares a property named <c>EqualityContract</c> and a constructor whose
    /// single parameter is the record struct itself has neither an equality contract nor a copy constructor,
    /// because the compiler synthesizes neither for a record struct.
    /// </summary>
    [Fact]
    public void RecordStructThatDeclaresMembersOfTheFacetHasNone()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( GetCode() );

        var facet = compilation.Types.OfName( "RecordStructWithOwnEqualityContractAndCopyConstructor" ).Single().Facets.Record;

        Assert.NotNull( facet );
        Assert.Null( facet.EqualityContractProperty );
        Assert.Null( facet.CloneMethod );
        Assert.Null( facet.CopyConstructor );

        Assert.Equal( "PrintMembers", facet.PrintMembersMethod.Name );
        Assert.NotNull( facet.DeconstructMethod );
    }

    /// <summary>
    /// Verifies the acceptance criterion that the facet is absent for a type that is not a record.
    /// </summary>
    [Fact]
    public void TypeThatIsNotARecordHasNoRecordFacet()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( GetCode() );

        foreach ( var typeName in new[] { "OrdinaryClass", "OrdinaryStruct", "IInterface", "Enum", "Handler" } )
        {
            var type = compilation.Types.OfName( typeName ).Single();

            Assert.False( type.IsRecord, $"{typeName} should not be a record." );
            Assert.Null( type.Facets.Record );
        }
    }

    /// <summary>
    /// Verifies that a record class declared in a referenced project reports every member of the facet, so that the
    /// facet does not depend on the record being declared in the compilation that reads it.
    /// </summary>
    [Fact]
    public void RecordFromReferencedProjectHasTheFacet()
    {
        using var testContext = this.CreateTestContext();

        // ReSharper disable once ConvertToConstant.Local
        var dependentCode = "public record ReferencedRecord( int Id, string Name );";

#if !NET5_0_OR_GREATER
        dependentCode += "namespace System.Runtime.CompilerServices { internal static class IsExternalInit {} }";
#endif

        var compilation = testContext.CreateCompilation( "class C { ReferencedRecord F = null!; }", dependentCode );

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
        // ReSharper disable once ConvertToConstant.Local
        var code = """
                   record Box<T>( T Value );

                   class Holder
                   {
                       public Box<string> ConstructedRecord = null!;
                   }
                   """;

#if !NET5_0_OR_GREATER
        code += "namespace System.Runtime.CompilerServices { internal static class IsExternalInit {} }";
#endif

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
        Assert.True( facet.CopyConstructor!.Parameters.Single().Type.Equals( constructedRecord, TypeComparison.Default ) );

        // The clone method is resolved from the symbol, so its mapping to the constructed type is verified too.
        Assert.True( facet.CloneMethod!.ReturnType.Equals( constructedRecord, TypeComparison.Default ) );
        Assert.Equal( constructedRecord, facet.CloneMethod.DeclaringType );

        // The generic definition reports the members that are not substituted.
        var definition = constructedRecord.Definition;

        Assert.Equal( definition.TypeParameters[0], definition.Facets.Record!.PositionalProperties.Single().Type );
    }
}
