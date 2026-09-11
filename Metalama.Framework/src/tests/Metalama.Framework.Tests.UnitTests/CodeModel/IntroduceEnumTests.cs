// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests of the builder of an introduced enum. See issue #866 and the design document
/// <c>Metalama.Framework/docs/future/introducing-enums.md</c>.
/// </summary>
public sealed class IntroduceEnumTests : UnitTestClass
{
    /// <summary>
    /// Creates an enum builder in a mutable clone of a compilation built from the given code.
    /// </summary>
    private static EnumBuilder CreateEnumBuilder( CompilationModel compilation, string name = "IntroducedEnum" )
        => new( null!, compilation.GlobalNamespace, name );

    /// <summary>
    /// Gets a special type as an <see cref="INamedType"/>, which is what <see cref="IEnumBuilder.UnderlyingType"/>
    /// takes.
    /// </summary>
    private static INamedType GetNamedType( CompilationModel compilation, SpecialType specialType )
        => compilation.Factory.GetSpecialType( specialType );

    /// <summary>
    /// Registers an enum and its members in the compilation and returns the introduced type. The members are
    /// registered as their own transformations, which is what makes them visible through
    /// <see cref="INamedType.Fields"/>.
    /// </summary>
    private static INamedType Introduce( CompilationModel compilation, EnumBuilder builder )
    {
        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        foreach ( var member in builder.MemberBuilders )
        {
            compilation.AddTransformation( new IntroduceSynthesizedDeclarationTransformation( null!, member.BuilderData ) );
        }

        return compilation.Types.OfName( builder.Name ).Single();
    }

    /// <summary>
    /// Verifies that an introduced enum reports the flags of an enum and that its default underlying type is
    /// <c>int</c>.
    /// </summary>
    [Fact]
    public void IntroducedEnumReportsTheFlagsOfAnEnum()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var introducedType = Introduce( compilation, CreateEnumBuilder( compilation ) );

        Assert.Equal( TypeKind.Enum, introducedType.TypeKind );
        Assert.True( introducedType.IsEnum );
        Assert.False( introducedType.IsReferenceType );
        Assert.False( introducedType.IsRecord );
        Assert.False( introducedType.IsDelegate );
        Assert.Equal( SpecialType.Int32, introducedType.UnderlyingType.SpecialType );
    }

    /// <summary>
    /// Verifies that the facet of an introduced enum reports the same shape as the facet of the equivalent enum read
    /// from source, which is the assertion that catches a facet built from builder data diverging from one built
    /// from a symbol.
    /// </summary>
    [Fact]
    public void FacetOfIntroducedEnumAgreesWithTheFacetOfASourceEnum()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext
            .CreateCompilationModel( "[System.Flags] enum SourceEnum : byte { None = 0, First = 1, Second = 2 }" )
            .CreateMutableClone();

        var builder = CreateEnumBuilder( compilation );
        ((IEnumBuilder) builder).UnderlyingType = GetNamedType( compilation, SpecialType.Byte );
        builder.IsFlags = true;
        builder.AddMember( "None", (byte) 0 );
        builder.AddMember( "First", (byte) 1 );
        builder.AddMember( "Second", (byte) 2 );

        var introducedType = Introduce( compilation, builder );

        var sourceFacet = compilation.Types.OfName( "SourceEnum" ).Single().Facets.Enum;
        var introducedFacet = introducedType.Facets.Enum;

        Assert.NotNull( sourceFacet );
        Assert.NotNull( introducedFacet );

        Assert.Equal( sourceFacet.IsFlags, introducedFacet.IsFlags );
        Assert.Equal( sourceFacet.UnderlyingType.SpecialType, introducedFacet.UnderlyingType.SpecialType );

        Assert.Equal(
            sourceFacet.Members.SelectAsArray( m => m.Name ),
            introducedFacet.Members.SelectAsArray( m => m.Name ) );

        Assert.Equal(
            sourceFacet.Members.SelectAsArray( m => m.ConstantValue!.Value.Value ),
            introducedFacet.Members.SelectAsArray( m => m.ConstantValue!.Value.Value ) );
    }

    /// <summary>
    /// Verifies that the members of an introduced enum are reported in the order in which they were added, and not
    /// in the order in which a consumer happened to resolve them by name.
    /// </summary>
    [Fact]
    public void MembersAreReportedInTheOrderInWhichTheyWereAdded()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreateEnumBuilder( compilation );
        builder.AddMember( "Third" );
        builder.AddMember( "First" );
        builder.AddMember( "Second" );

        var introducedType = Introduce( compilation, builder );

        // A field is resolved by name before the facet is read, which is what perturbs the order of
        // INamedType.Fields.
        _ = introducedType.Fields.OfName( "Second" ).Single();

        Assert.Equal(
            new[] { "Third", "First", "Second" },
            introducedType.Facets.Enum!.Members.SelectAsArray( m => m.Name ) );
    }

    /// <summary>
    /// Verifies that a member of an introduced enum is a public constant field whose type is the enum, which is what
    /// the language declares and what an aspect reading the code model expects.
    /// </summary>
    [Fact]
    public void MemberOfIntroducedEnumIsAConstantFieldOfTheEnumType()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreateEnumBuilder( compilation );
        builder.AddMember( "First", 1 );

        var introducedType = Introduce( compilation, builder );

        var field = introducedType.Fields.OfName( "First" ).Single();

        Assert.Equal( Accessibility.Public, field.Accessibility );
        Assert.True( field.IsStatic );
        Assert.Equal( Writeability.None, field.Writeability );
        Assert.Same( introducedType, field.Type );
        Assert.Equal( 1, (int) field.ConstantValue!.Value.Value! );
    }

    /// <summary>
    /// Verifies that every integral type the language allows is accepted as the underlying type and that any other
    /// type is refused.
    /// </summary>
    [Theory]
    [InlineData( SpecialType.Byte, true )]
    [InlineData( SpecialType.SByte, true )]
    [InlineData( SpecialType.Int16, true )]
    [InlineData( SpecialType.UInt16, true )]
    [InlineData( SpecialType.Int32, true )]
    [InlineData( SpecialType.UInt32, true )]
    [InlineData( SpecialType.Int64, true )]
    [InlineData( SpecialType.UInt64, true )]
    [InlineData( SpecialType.String, false )]
    [InlineData( SpecialType.Boolean, false )]
    [InlineData( SpecialType.Decimal, false )]
    [InlineData( SpecialType.Object, false )]
    public void UnderlyingTypeIsValidated( SpecialType specialType, bool isValid )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = (IEnumBuilder) CreateEnumBuilder( compilation );
        var type = GetNamedType( compilation, specialType );

        if ( isValid )
        {
            builder.UnderlyingType = type;
            Assert.Equal( specialType, builder.UnderlyingType.SpecialType );
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>( () => builder.UnderlyingType = type );
        }
    }

    /// <summary>
    /// Verifies that the underlying type cannot be changed once a member has been added, because the value of a
    /// member is validated against the underlying type when the member is added.
    /// </summary>
    [Fact]
    public void UnderlyingTypeCannotBeChangedAfterAMemberIsAdded()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = (IEnumBuilder) CreateEnumBuilder( compilation );
        builder.AddMember( "First" );

        Assert.Throws<InvalidOperationException>(
            () => builder.UnderlyingType = GetNamedType( compilation, SpecialType.Byte ) );
    }

    /// <summary>
    /// Verifies that a value that does not fit in the underlying type is refused.
    /// </summary>
    [Fact]
    public void ValueThatDoesNotFitInTheUnderlyingTypeIsRefused()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = (IEnumBuilder) CreateEnumBuilder( compilation );
        builder.UnderlyingType = GetNamedType( compilation, SpecialType.Byte );

        Assert.Throws<ArgumentOutOfRangeException>( () => builder.AddMember( "TooLarge", 256 ) );
        Assert.Throws<ArgumentOutOfRangeException>( () => builder.AddMember( "Negative", -1 ) );

        // The bounds themselves are accepted.
        builder.AddMember( "Zero", 0 );
        builder.AddMember( "Max", 255 );
    }

    /// <summary>
    /// Verifies that the most negative value of a signed underlying type is accepted. The magnitude of that value is
    /// one more than the largest positive value, which is why it is computed in unsigned arithmetic rather than
    /// through <see cref="Math.Abs(long)"/>.
    /// </summary>
    [Fact]
    public void MostNegativeValueOfASignedUnderlyingTypeIsAccepted()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var enumBuilder = CreateEnumBuilder( compilation );
        var builder = (IEnumBuilder) enumBuilder;
        builder.UnderlyingType = GetNamedType( compilation, SpecialType.Int64 );

        builder.AddMember( "Min", long.MinValue );
        builder.AddMember( "Max", long.MaxValue );

        var introducedType = Introduce( compilation, enumBuilder );

        Assert.Equal( long.MinValue, (long) introducedType.Fields.OfName( "Min" ).Single().ConstantValue!.Value.Value! );
        Assert.Equal( long.MaxValue, (long) introducedType.Fields.OfName( "Max" ).Single().ConstantValue!.Value.Value! );
    }

    /// <summary>
    /// Verifies that a duplicate member name is refused.
    /// </summary>
    [Fact]
    public void DuplicateMemberNameIsRefused()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = (IEnumBuilder) CreateEnumBuilder( compilation );
        builder.AddMember( "First" );

        Assert.Throws<ArgumentException>( () => builder.AddMember( "First" ) );
        Assert.Throws<ArgumentException>( () => builder.AddMember( "First", 1 ) );
    }

    /// <summary>
    /// Verifies that <see cref="IEnumBuilder.IsFlags"/> and
    /// <see cref="IDeclarationBuilder.AddAttribute"/> are one state and not two.
    /// </summary>
    [Fact]
    public void IsFlagsReportsTheAttribute()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = (IEnumBuilder) CreateEnumBuilder( compilation );

        Assert.False( builder.IsFlags );

        builder.IsFlags = true;
        Assert.True( builder.IsFlags );
        Assert.Single( builder.Attributes );

        // Setting the property twice does not add the attribute twice.
        builder.IsFlags = true;
        Assert.Single( builder.Attributes );

        builder.IsFlags = false;
        Assert.False( builder.IsFlags );
        Assert.Empty( builder.Attributes );
    }

    /// <summary>
    /// Verifies that the four modifiers that an enum cannot carry throw a <see cref="NotSupportedException"/>, which
    /// is the rule of section 5.1 of <c>Metalama.Framework/docs/future/introducing-types.md</c>.
    /// </summary>
    [Fact]
    public void ModifiersThatAnEnumDoesNotHaveAreRefused()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = (IEnumBuilder) CreateEnumBuilder( compilation );

        Assert.Throws<NotSupportedException>( () => builder.IsStatic = true );
        Assert.Throws<NotSupportedException>( () => builder.IsAbstract = true );
        Assert.Throws<NotSupportedException>( () => builder.IsSealed = false );
        Assert.Throws<NotSupportedException>( () => builder.IsPartial = true );

        // An enum is implicitly sealed, and the code model reports it the way Roslyn reports an enum read from
        // source.
        Assert.True( builder.IsSealed );
        Assert.False( builder.IsStatic );
        Assert.False( builder.IsAbstract );
    }

    /// <summary>
    /// Verifies that the builder refuses every operation once it is frozen.
    /// </summary>
    [Fact]
    public void FrozenBuilderRefusesEveryChange()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var enumBuilder = CreateEnumBuilder( compilation );
        var builder = (IEnumBuilder) enumBuilder;
        builder.AddMember( "First" );
        enumBuilder.Freeze();

        Assert.Throws<InvalidOperationException>( () => builder.AddMember( "Second" ) );
        Assert.Throws<InvalidOperationException>( () => builder.IsFlags = true );

        Assert.Throws<InvalidOperationException>(
            () => builder.UnderlyingType = GetNamedType( compilation, SpecialType.Byte ) );
    }

    /// <summary>
    /// Verifies that <see cref="INamedType.Facets"/> of an enum builder throws, while the flags that the collection
    /// dispatches on answer without throwing. See section 5.1 of
    /// <c>Metalama.Framework/docs/future/introducing-types.md</c>.
    /// </summary>
    [Fact]
    public void FacetsOfEnumBuilderThrow()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreateEnumBuilder( compilation );

        Assert.True( builder.IsEnum );
        Assert.Throws<NotSupportedException>( () => builder.Facets );
    }
}
