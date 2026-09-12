// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.Transformations;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests of <see cref="IntroduceSynthesizedDeclarationTransformation"/>, which registers a declaration in the code
/// model without emitting any syntax for it. See section 4.2 of the design document
/// <c>Metalama.Framework/docs/introducing-types.md</c>.
/// </summary>
/// <remarks>
/// <para>
/// The five type introduction issues depend on this shape, because the compiler synthesizes the members of a
/// delegate, a record, a union and a struct from the declaration that Metalama emits, so those members have to
/// reach the code model without being emitted a second time. Story S-29 asks whether a member builder with no
/// injected member survives the pipeline, and these tests are the answer.
/// </para>
/// </remarks>
public sealed class SynthesizedDeclarationTests : UnitTestClass
{
    /// <summary>
    /// Verifies that a constructor registered by <see cref="IntroduceSynthesizedDeclarationTransformation"/> reaches
    /// the member collection of its declaring type, which is what an aspect reads.
    /// </summary>
    [Fact]
    public void SynthesizedConstructorIsVisibleInTheCodeModel()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var typeBuilder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", TypeKind.Class );
        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        var introducedType = compilation.Types.OfName( "IntroducedType" ).Single();

        var constructorBuilder = new ConstructorBuilder( null!, introducedType, isImplicitlyDeclared: true )
        {
            Accessibility = Accessibility.Public
        };

        constructorBuilder.Freeze();

        compilation.AddTransformation(
            new IntroduceSynthesizedDeclarationTransformation( null!, constructorBuilder.BuilderData ) );

        var constructor = Assert.Single( introducedType.Constructors );
        Assert.Equal( Accessibility.Public, constructor.Accessibility );
    }

    /// <summary>
    /// Verifies that the transformation does not implement <see cref="IInjectMemberTransformation"/>, which is the
    /// mechanism by which the member is kept out of the generated code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both emitters are keyed on that interface, so the assertion covers the build-time linker and the design-time
    /// generator at once. A change that made this transformation implement the interface would emit every
    /// synthesized member a second time, and the compiler would report a duplicate declaration on generated code
    /// that the user cannot edit, so the property is asserted here rather than left to the baselines.
    /// </para>
    /// </remarks>
    [Fact]
    public void SynthesizedDeclarationIsNotInjected()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var typeBuilder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", TypeKind.Class );
        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        var introducedType = compilation.Types.OfName( "IntroducedType" ).Single();

        var constructorBuilder = new ConstructorBuilder( null!, introducedType, isImplicitlyDeclared: true );
        constructorBuilder.Freeze();

        var transformation = new IntroduceSynthesizedDeclarationTransformation( null!, constructorBuilder.BuilderData );

        Assert.IsNotAssignableFrom<IInjectMemberTransformation>( transformation );
        Assert.IsAssignableFrom<IIntroduceDeclarationTransformation>( transformation );

        // The observability is what puts the declaration in the code model. A transformation whose observability is
        // None is ignored by CompilationModel.AddTransformation.
        Assert.Equal( TransformationObservability.Always, transformation.Observability );
    }
}
