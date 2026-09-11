// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests of the builder of an introduced record. See issue #867 and the design document
/// <c>Metalama.Framework/docs/future/introducing-records.md</c>.
/// </summary>
/// <remarks>
/// <para>
/// The members that the compiler synthesizes for a record are registered in the code model and are emitted by
/// nothing, so an aspect test cannot see them at all. Section 5 of the design document states that this kind is the
/// one for which that difference matters most, and these tests are what proves that the members exist.
/// </para>
/// </remarks>
public sealed class IntroduceRecordTests : UnitTestClass
{
    /// <summary>
    /// Registers a record and every member the compiler synthesizes for it, and returns the introduced type.
    /// </summary>
    private static INamedType Introduce( CompilationModel compilation, RecordBuilder builder )
    {
        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        foreach ( var member in builder.GetSynthesizedMemberData() )
        {
            compilation.AddTransformation( new IntroduceSynthesizedDeclarationTransformation( null!, member ) );
        }

        return compilation.Types.OfName( builder.Name ).Single();
    }

    /// <summary>
    /// Builds a positional record of the given authoring form with two parameters.
    /// </summary>
    private static RecordBuilder CreatePositionalRecord( CompilationModel compilation, RecordKind recordKind, string name = "IntroducedRecord" )
    {
        var builder = new RecordBuilder( null!, compilation.GlobalNamespace, name, recordKind );

        builder.AddPositionalParameter( "Name", compilation.Factory.GetSpecialType( SpecialType.String ) );
        builder.AddPositionalParameter( "Count", compilation.Factory.GetSpecialType( SpecialType.Int32 ) );

        return builder;
    }

    /// <summary>
    /// Verifies that an introduced record reports the kind and the flags of a record in both authoring forms.
    /// </summary>
    [Theory]
    [InlineData( RecordKind.Class, TypeKind.Class )]
    [InlineData( RecordKind.Struct, TypeKind.Struct )]
    public void IntroducedRecordReportsTheFlagsOfARecord( RecordKind recordKind, TypeKind typeKind )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var introducedType = Introduce( compilation, CreatePositionalRecord( compilation, recordKind ) );

        Assert.Equal( typeKind, introducedType.TypeKind );
        Assert.True( introducedType.IsRecord );
        Assert.False( introducedType.IsEnum );
        Assert.False( introducedType.IsDelegate );
        Assert.Equal( typeKind == TypeKind.Class, introducedType.IsReferenceType );
    }

    /// <summary>
    /// Verifies that the facet of an introduced record reports the same six members as the facet of the equivalent
    /// record read from source, with the same three of them null for a record struct. This is the central assertion
    /// of the issue.
    /// </summary>
    [Theory]
    [InlineData( RecordKind.Class, "record SourceRecord( string Name, int Count );" )]
    [InlineData( RecordKind.Struct, "record struct SourceRecord( string Name, int Count );" )]
    public void FacetOfIntroducedRecordAgreesWithTheFacetOfASourceRecord( RecordKind recordKind, string sourceCode )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( sourceCode ).CreateMutableClone();

        var introducedType = Introduce( compilation, CreatePositionalRecord( compilation, recordKind ) );

        var sourceFacet = compilation.Types.OfName( "SourceRecord" ).Single().Facets.Record;
        var introducedFacet = introducedType.Facets.Record;

        Assert.NotNull( sourceFacet );
        Assert.NotNull( introducedFacet );

        // The three members that a record class has and a record struct does not.
        Assert.Equal( sourceFacet.EqualityContractProperty == null, introducedFacet.EqualityContractProperty == null );
        Assert.Equal( sourceFacet.CloneMethod == null, introducedFacet.CloneMethod == null );
        Assert.Equal( sourceFacet.CopyConstructor == null, introducedFacet.CopyConstructor == null );

        // The three that both forms have.
        Assert.NotNull( sourceFacet.PrintMembersMethod );
        Assert.NotNull( introducedFacet.PrintMembersMethod );
        Assert.NotNull( sourceFacet.DeconstructMethod );
        Assert.NotNull( introducedFacet.DeconstructMethod );

        Assert.Equal(
            sourceFacet.PositionalProperties.SelectAsArray( p => p.Name ),
            introducedFacet.PositionalProperties.SelectAsArray( p => p.Name ) );

        Assert.Equal(
            sourceFacet.PositionalProperties.SelectAsArray( p => p.Type.SpecialType ),
            introducedFacet.PositionalProperties.SelectAsArray( p => p.Type.SpecialType ) );
    }

    /// <summary>
    /// Verifies that <c>CloneMethod</c> is not null for an introduced record class. It returned <c>null</c> before
    /// this issue, and silently, because it read the symbol of the type and an introduced type has none.
    /// </summary>
    [Fact]
    public void CloneMethodOfIntroducedRecordClassIsNotNull()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var introducedType = Introduce( compilation, CreatePositionalRecord( compilation, RecordKind.Class ) );

        var cloneMethod = introducedType.Facets.Record!.CloneMethod;

        Assert.NotNull( cloneMethod );
        Assert.Equal( "<Clone>$", cloneMethod.Name );

        // The name of the clone method is not a C# identifier, so it is absent from INamedType.Methods and is
        // reached through the facet alone.
        Assert.Empty( introducedType.Methods.OfName( "<Clone>$" ) );
    }

    /// <summary>
    /// Verifies that <c>PositionalProperties</c> is not empty for an introduced record. It returned an empty list
    /// before this issue, and silently, because it told a positional property from an ordinary one by its declaring
    /// syntax and an introduced property has none.
    /// </summary>
    [Theory]
    [InlineData( RecordKind.Class )]
    [InlineData( RecordKind.Struct )]
    public void PositionalPropertiesOfIntroducedRecordAreNotEmpty( RecordKind recordKind )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var introducedType = Introduce( compilation, CreatePositionalRecord( compilation, recordKind ) );

        var positionalProperties = introducedType.Facets.Record!.PositionalProperties;

        Assert.Equal( 2, positionalProperties.Count );
        Assert.Equal( "Name", positionalProperties[0].Name );
        Assert.Equal( "Count", positionalProperties[1].Name );
        Assert.Equal( SpecialType.String, positionalProperties[0].Type.SpecialType );
        Assert.Equal( SpecialType.Int32, positionalProperties[1].Type.SpecialType );

        // A positional parameter declares a public property that can be written by an initializer only, which is
        // what a positional property of a record read from source reports.
        Assert.All( positionalProperties, p => Assert.Equal( Accessibility.Public, p.Accessibility ) );
        Assert.All( positionalProperties, p => Assert.Equal( Writeability.ConstructorOnly, p.Writeability ) );
    }

    /// <summary>
    /// Verifies that a record that declares no positional parameter has no deconstructing method and no positional
    /// property, which is what the language gives it.
    /// </summary>
    [Fact]
    public void NonPositionalRecordHasNoDeconstructMethod()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new RecordBuilder( null!, compilation.GlobalNamespace, "NonPositional", RecordKind.Class );
        var introducedType = Introduce( compilation, builder );

        var facet = introducedType.Facets.Record!;

        Assert.Null( facet.DeconstructMethod );
        Assert.Empty( facet.PositionalProperties );

        // Every other member is still synthesized.
        Assert.NotNull( facet.EqualityContractProperty );
        Assert.NotNull( facet.PrintMembersMethod );
        Assert.NotNull( facet.CloneMethod );
        Assert.NotNull( facet.CopyConstructor );
    }

    /// <summary>
    /// Verifies that the primary constructor of an introduced record is reported by
    /// <see cref="INamedType.PrimaryConstructor"/>, which is where <c>IRecordFacet</c> says it is reached, and that
    /// its parameters are the positional parameters.
    /// </summary>
    [Fact]
    public void PrimaryConstructorIsReportedAndCarriesThePositionalParameters()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var introducedType = Introduce( compilation, CreatePositionalRecord( compilation, RecordKind.Class ) );

        var primaryConstructor = introducedType.PrimaryConstructor;

        Assert.NotNull( primaryConstructor );
        Assert.True( primaryConstructor.IsPrimary );
        Assert.Equal( 2, primaryConstructor.Parameters.Count );
        Assert.Equal( "Name", primaryConstructor.Parameters[0].Name );
        Assert.Equal( "Count", primaryConstructor.Parameters[1].Name );
    }

    /// <summary>
    /// Verifies that <c>PrintMembers</c> is <c>protected virtual</c> on a record class that is not sealed and
    /// <c>private</c> on one that is, and always <c>private</c> on a record struct.
    /// </summary>
    [Fact]
    public void AccessibilityOfPrintMembersFollowsTheKindAndTheSealedModifier()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var unsealedClass = Introduce( compilation, CreatePositionalRecord( compilation, RecordKind.Class, "UnsealedClass" ) );

        var sealedClassBuilder = CreatePositionalRecord( compilation, RecordKind.Class, "SealedClass" );
        sealedClassBuilder.IsSealed = true;
        var sealedClass = Introduce( compilation, sealedClassBuilder );

        var recordStruct = Introduce( compilation, CreatePositionalRecord( compilation, RecordKind.Struct, "Struct" ) );

        var unsealedPrintMembers = unsealedClass.Facets.Record!.PrintMembersMethod;
        Assert.Equal( Accessibility.Protected, unsealedPrintMembers.Accessibility );
        Assert.True( unsealedPrintMembers.IsVirtual );

        Assert.Equal( Accessibility.Private, sealedClass.Facets.Record!.PrintMembersMethod.Accessibility );
        Assert.Equal( Accessibility.Private, recordStruct.Facets.Record!.PrintMembersMethod.Accessibility );
    }

    /// <summary>
    /// Verifies that the members implementing equality, which are not part of the facet, are reported by
    /// <see cref="INamedType.Methods"/>, which section 5 of the design document requires.
    /// </summary>
    [Fact]
    public void EqualityMembersAreReportedByTheMemberCollections()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var introducedType = Introduce( compilation, CreatePositionalRecord( compilation, RecordKind.Class ) );

        Assert.Equal( 2, introducedType.Methods.OfName( "Equals" ).Count() );
        Assert.Single( introducedType.Methods.OfName( nameof(this.GetHashCode) ) );
        Assert.Single( introducedType.Methods.OfName( nameof(this.ToString) ) );

        var operators = introducedType.Methods.Where( m => m.OperatorKind is OperatorKind.Equality or OperatorKind.Inequality ).ToArray();

        Assert.Equal( 2, operators.Length );
        Assert.All( operators, op => Assert.True( op.IsStatic ) );
        Assert.All( operators, op => Assert.Equal( Accessibility.Public, op.Accessibility ) );
    }

    /// <summary>
    /// Verifies that the builder refuses a positional parameter once it is frozen.
    /// </summary>
    [Fact]
    public void FrozenBuilderRefusesAPositionalParameter()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreatePositionalRecord( compilation, RecordKind.Class );
        builder.Freeze();

        Assert.Throws<InvalidOperationException>(
            () => builder.AddPositionalParameter( "Extra", compilation.Factory.GetSpecialType( SpecialType.Int32 ) ) );
    }

    /// <summary>
    /// Verifies that <see cref="INamedType.Facets"/> of a record builder throws, while
    /// <see cref="INamedType.IsRecord"/> answers without throwing. See section 5.1 of
    /// <c>Metalama.Framework/docs/future/introducing-types.md</c>.
    /// </summary>
    [Fact]
    public void FacetsOfRecordBuilderThrow()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreatePositionalRecord( compilation, RecordKind.Class );

        Assert.True( builder.IsRecord );
        Assert.Throws<NotSupportedException>( () => builder.Facets );
    }
}
