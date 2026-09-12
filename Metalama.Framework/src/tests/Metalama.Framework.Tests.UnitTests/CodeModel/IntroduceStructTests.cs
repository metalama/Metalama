// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests of the builder of an introduced struct. See issue #869 and the design document
/// <c>Metalama.Framework/docs/introducing-structs.md</c>.
/// </summary>
public sealed class IntroduceStructTests : UnitTestClass
{
    /// <summary>
    /// Verifies that a struct builder reports the flags of a value type. <c>IsReferenceType</c> decides what
    /// <c>ToNullable</c> produces, so a struct reported as a reference type would give an annotated reference type
    /// where the language requires <c>Nullable&lt;T&gt;</c>.
    /// </summary>
    [Fact]
    public void IntroducedStructIsAValueType()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedStruct", TypeKind.Struct );
        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        var introducedType = compilation.Types.OfName( "IntroducedStruct" ).Single();

        Assert.Equal( TypeKind.Struct, introducedType.TypeKind );
        Assert.False( introducedType.IsReferenceType );
        Assert.False( introducedType.IsRecord );
        Assert.False( introducedType.IsEnum );
        Assert.False( introducedType.IsDelegate );

        // A struct has no facet, so the collection is empty rather than absent.
        Assert.Empty( introducedType.Facets );
    }

    /// <summary>
    /// Verifies that the nullable form of an introduced struct is a nullable value type and not an annotated
    /// reference type, which is how a struct read from source answers. See #1840.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ToNullable</c> branches on <c>IsReferenceType</c>, so this assertion is what a defect in that property
    /// would break. <c>CodeModelConsistencyTests.ToNullableOnAValueTypeAgreesBetweenSourceAndIntroduced</c> compares
    /// the two forms in more detail; this test pins the one property that the branch reads.
    /// </para>
    /// </remarks>
    [Fact]
    public void NullableIntroducedStructIsAValueType()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "struct SourceStruct;" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedStruct", TypeKind.Struct );
        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        var sourceNullable = compilation.Types.OfName( "SourceStruct" ).Single().ToNullable();
        var introducedNullable = compilation.Types.OfName( "IntroducedStruct" ).Single().ToNullable();

        Assert.Equal( sourceNullable.IsNullable, introducedNullable.IsNullable );
        Assert.Equal( sourceNullable.IsReferenceType, introducedNullable.IsReferenceType );
    }

    /// <summary>
    /// Verifies that <see cref="Metalama.Framework.Code.DeclarationBuilders.INamedTypeBuilder.IsReadOnly"/> and
    /// <see cref="Metalama.Framework.Code.DeclarationBuilders.INamedTypeBuilder.IsRef"/> are accepted on a struct and
    /// refused on every other kind, which is the rule of the language.
    /// </summary>
    [Theory]
    [InlineData( TypeKind.Class )]
    [InlineData( TypeKind.Interface )]
    [InlineData( TypeKind.Enum )]
    [InlineData( TypeKind.Delegate )]
    public void ReadOnlyAndRefAreRefusedForTypesThatAreNotStructs( TypeKind typeKind )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", typeKind );

        Assert.Throws<InvalidOperationException>( () => builder.IsReadOnly = true );
        Assert.Throws<InvalidOperationException>( () => builder.IsRef = true );
    }

    /// <summary>
    /// Verifies that a struct accepts both modifiers and reports them on the introduced type.
    /// </summary>
    [Fact]
    public void ReadOnlyAndRefAreAcceptedForAStruct()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedStruct", TypeKind.Struct )
        {
            IsReadOnly = true,
            IsRef = true
        };

        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        var introducedType = compilation.Types.OfName( "IntroducedStruct" ).Single();

        Assert.True( introducedType.IsReadOnly );
        Assert.True( introducedType.IsRef );
    }
}
