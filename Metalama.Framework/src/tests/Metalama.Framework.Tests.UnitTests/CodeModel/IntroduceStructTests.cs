// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Abstractions;
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
    /// Verifies that an introduced type of an implicitly sealed kind reports itself as sealed and as not inheritable,
    /// which is what a type read from source reports.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A struct, an enum and a delegate are sealed whether or not the aspect says so, and no type is ever derived
    /// from a value type, so <c>CanBeInherited</c> is false for all three. An aspect reads that property to decide
    /// whether it has to consider a derived type it cannot see.
    /// </para>
    /// </remarks>
    [Fact]
    public void ImplicitlySealedKindsAreSealedAndNotInheritable()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "public struct SourceStruct { } public enum SourceEnum { None } public delegate void SourceDelegate();" )
            .CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedStruct", TypeKind.Struct );
        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        var introduced = compilation.Types.OfName( "IntroducedStruct" ).Single();
        var source = compilation.Types.OfName( "SourceStruct" ).Single();

        Assert.True( introduced.IsSealed );
        Assert.Equal( source.IsSealed, introduced.IsSealed );
        Assert.Equal( ((IDeclarationImpl) source).CanBeInherited, ((IDeclarationImpl) introduced).CanBeInherited );
        Assert.False( ((IDeclarationImpl) introduced).CanBeInherited );
    }

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
    /// Verifies that an introduced type that carries no nullable annotation still carries none after a round trip
    /// through its reference, which is how the type reaches another compilation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Metalama.Framework.Code.IType.StripNullabilityAnnotation"/> produces the state in which the type
    /// is neither annotated nor explicitly non-annotated, which the code model reports as <c>null</c>. The reference
    /// carries the three states and not the two of a boolean, because a type that came back explicitly
    /// non-nullable would report a nullability that the aspect did not ask for.
    /// </para>
    /// </remarks>
    [Fact]
    public void TypeWithoutNullableAnnotationKeepsItThroughItsReference()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedClass", TypeKind.Class );
        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        var introducedType = compilation.Types.OfName( "IntroducedClass" ).Single();

        // The three states, each of which has to survive the round trip.
        foreach ( var type in new[] { introducedType.StripNullabilityAnnotation(), introducedType.ToNullable(), introducedType.ToNonNullable() } )
        {
            var roundTripped = (INamedType) type.ToRef().GetTarget( compilation );

            Assert.Equal( type.IsNullable, roundTripped.IsNullable );
        }

        Assert.Null( introducedType.StripNullabilityAnnotation().IsNullable );
        Assert.True( introducedType.ToNullable().IsNullable );
        Assert.False( introducedType.ToNonNullable().IsNullable );
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
