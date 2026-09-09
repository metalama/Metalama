// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests of <see cref="INamedType.IsEnum"/> and <see cref="IEnumFacet"/>. See issue #1996 and the design document
/// <c>Metalama.Framework/docs/future/type-facets.md</c>.
/// </summary>
public sealed class EnumFacetTests : UnitTestClass
{
    private const string _code = """
                                 using System;

                                 enum Color { Red, Green = 5, Blue }

                                 enum SmallEnum : byte { First = 1, Second = 2 }

                                 [Flags]
                                 enum Permissions { None = 0, Read = 1, Write = 2, All = Read | Write }

                                 class OrdinaryClass;
                                 struct Struct;
                                 interface IInterface;
                                 delegate void Handler();
                                 record RecordClass;

                                 class Holder
                                 {
                                     public Color? NullableEnum;
                                     public int? NullableInt;
                                     public string NullableReference = null!;
                                 }
                                 """;

    /// <summary>
    /// Verifies that an enum reports <see cref="INamedType.IsEnum"/>, that the collection contains exactly the enum
    /// facet, and that the facet names the type it belongs to.
    /// </summary>
    [Fact]
    public void EnumHasTheEnumFacet()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var color = compilation.Types.OfName( "Color" ).Single();

        Assert.True( color.IsEnum );

        var facet = color.Facets.Enum;

        Assert.NotNull( facet );
        Assert.Equal( TypeFacetKind.Enum, facet.FacetKind );
        Assert.Same( color, facet.Type );
        Assert.Null( color.Facets.Delegate );

        // The count is read into a local because the invariant is that it agrees with the typed properties, which is
        // a different statement from the single element asserted below.
        var facetCount = color.Facets.Count;

        Assert.Equal( 1, facetCount );
        Assert.Same( facet, Assert.Single( color.Facets ) );
    }

    /// <summary>
    /// Verifies the acceptance criterion that the underlying type reported by the facet equals the one reported by
    /// <see cref="INamedType.UnderlyingType"/>, for an enum whose underlying type is the default one and for an enum
    /// that declares another one.
    /// </summary>
    [Fact]
    public void UnderlyingTypeOfFacetEqualsUnderlyingTypeOfNamedType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var color = compilation.Types.OfName( "Color" ).Single();
        var smallEnum = compilation.Types.OfName( "SmallEnum" ).Single();

        Assert.Equal( SpecialType.Int32, color.Facets.Enum!.UnderlyingType.SpecialType );
        Assert.Equal( color.UnderlyingType, color.Facets.Enum!.UnderlyingType );

        Assert.Equal( SpecialType.Byte, smallEnum.Facets.Enum!.UnderlyingType.SpecialType );
        Assert.Equal( smallEnum.UnderlyingType, smallEnum.Facets.Enum!.UnderlyingType );
    }

    /// <summary>
    /// Verifies the acceptance criterion that the members reported by the facet are in declaration order and that
    /// their constant values are readable, including the implicit value of a member that declares none.
    /// </summary>
    [Fact]
    public void MembersAreInDeclarationOrderAndHaveConstantValues()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var color = compilation.Types.OfName( "Color" ).Single();

        // The last member is resolved by name first. INamedType.Fields enumerates the fields that a previous consumer
        // resolved by name before the others, so this is the case in which an implementation that took its order from
        // that collection would report Blue first.
        _ = color.Fields.OfName( "Blue" ).Single();

        var members = color.Facets.Enum!.Members;

        Assert.Equal( new[] { "Red", "Green", "Blue" }, members.SelectAsArray( f => f.Name ) );
        Assert.Equal( new object?[] { 0, 5, 6 }, members.SelectAsArray( f => f.ConstantValue!.Value.Value ) );
    }

    /// <summary>
    /// Verifies that the members of the facet are the constants of the enum, and do not include the instance field
    /// named <c>value__</c> that carries the underlying value. That field is a member of the enum in metadata, so it
    /// appears in <see cref="INamedType.Fields"/>.
    /// </summary>
    [Fact]
    public void MembersDoNotIncludeTheUnderlyingValueField()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        var color = compilation.Types.OfName( "Color" ).Single();

        Assert.DoesNotContain( color.Facets.Enum!.Members, f => f.Name == "value__" );
        Assert.All( color.Facets.Enum!.Members, f => Assert.True( f.IsStatic ) );
    }

    /// <summary>
    /// Verifies that <see cref="IEnumFacet.IsFlags"/> reports the presence of <see cref="FlagsAttribute"/>.
    /// </summary>
    [Fact]
    public void IsFlagsReportsTheAttribute()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        Assert.True( compilation.Types.OfName( "Permissions" ).Single().Facets.Enum!.IsFlags );
        Assert.False( compilation.Types.OfName( "Color" ).Single().Facets.Enum!.IsFlags );
    }

    /// <summary>
    /// Verifies the acceptance criterion that the facet is absent for a type that is not an enum, and in particular
    /// for a nullable value type, for which <see cref="INamedType.UnderlyingType"/> returns a type that is not the
    /// underlying type of an enum.
    /// </summary>
    [Fact]
    public void TypeThatIsNotAnEnumHasNoEnumFacet()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );

        foreach ( var typeName in new[] { "OrdinaryClass", "Struct", "IInterface", "Handler", "RecordClass", "Holder" } )
        {
            var type = compilation.Types.OfName( typeName ).Single();

            Assert.False( type.IsEnum, $"{typeName} should not be an enum." );
            Assert.Null( type.Facets.Enum );
        }

        var holder = compilation.Types.OfName( "Holder" ).Single();

        foreach ( var fieldName in new[] { "NullableEnum", "NullableInt", "NullableReference" } )
        {
            var type = (INamedType) holder.Fields.OfName( fieldName ).Single().Type;

            // The type reports an underlying type, which is the shape that makes INamedType.UnderlyingType ambiguous.
            Assert.NotNull( type.UnderlyingType );

            Assert.False( type.IsEnum, $"The type of {fieldName} should not be an enum." );
            Assert.Null( type.Facets.Enum );
            Assert.Empty( type.Facets );
        }
    }

    /// <summary>
    /// Verifies that an enum read from a referenced assembly has the facet, with its members in declaration order and
    /// with <see cref="IEnumFacet.IsFlags"/> reporting the attribute that the metadata carries.
    /// </summary>
    [Fact]
    public void EnumFromReferencedAssemblyHasTheFacet()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );

        var consoleColor = compilation.Factory.GetNamedTypeByReflectionType( typeof(ConsoleColor) );

        Assert.True( consoleColor.IsEnum );

        var facet = consoleColor.Facets.Enum;

        Assert.NotNull( facet );
        Assert.Equal( SpecialType.Int32, facet.UnderlyingType.SpecialType );
        Assert.Equal( consoleColor.UnderlyingType, facet.UnderlyingType );
        Assert.False( facet.IsFlags );
        Assert.Equal( nameof(ConsoleColor.Black), facet.Members[0].Name );
        Assert.Equal( (int) ConsoleColor.Blue, facet.Members.Single( f => f.Name == nameof(ConsoleColor.Blue) ).ConstantValue!.Value.Value );

        var attributeTargets = compilation.Factory.GetNamedTypeByReflectionType( typeof(AttributeTargets) );

        Assert.True( attributeTargets.Facets.Enum!.IsFlags );
    }
}
