// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Testing.UnitTesting;
using System;
using Xunit;
#if ROSLYN_5_11_0_OR_GREATER
using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using System.Linq;
#endif

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests of the builder of an introduced union. See issue #1951 and the design document
/// <c>Metalama.Framework/docs/introducing-unions.md</c>.
/// </summary>
/// <remarks>
/// <para>
/// The builder refuses the union flag on the Roslyn variant that cannot emit a union declaration, so the tests that
/// build a union are compiled into the other variant, and this variant pins the refusal instead. The aspect test
/// that covers the emission lives under <c>Tests/Aspects/CSharp15/Unions</c>.
/// </para>
/// </remarks>
public sealed class IntroduceUnionTests : UnitTestClass
{
#if ROSLYN_5_11_0_OR_GREATER

    /// <summary>
    /// Registers a union and the members that the compiler synthesizes for it, and returns the introduced type.
    /// </summary>
    private static INamedType Introduce( CompilationModel compilation, UnionBuilder builder )
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
    /// Builds a union with an <c>int</c> case and a <c>string</c> case.
    /// </summary>
    private static UnionBuilder CreateUnionBuilder( CompilationModel compilation, string name = "IntroducedUnion" )
    {
        var builder = new UnionBuilder( null!, compilation.GlobalNamespace, name );

        builder.AddCase( compilation.Factory.GetSpecialType( SpecialType.Int32 ) );
        builder.AddCase( compilation.Factory.GetSpecialType( SpecialType.String ) );

        return builder;
    }

    /// <summary>
    /// Verifies that an introduced union reports the flags of a union. The language reports a union declaration as
    /// a struct, so the union flag is what tells it apart.
    /// </summary>
    [Fact]
    public void IntroducedUnionReportsTheFlagsOfAUnion()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var introducedType = Introduce( compilation, CreateUnionBuilder( compilation ) );

        Assert.Equal( TypeKind.Struct, introducedType.TypeKind );
        Assert.True( introducedType.IsUnion );
        Assert.False( introducedType.IsRecord );
        Assert.False( introducedType.IsEnum );
        Assert.False( introducedType.IsReferenceType );
    }

    /// <summary>
    /// Verifies that the facet of an introduced union reports the declaration form. It reported the attribute form
    /// before this issue, and silently, because it read the declaring syntax of the symbol of the type and an
    /// introduced type has none.
    /// </summary>
    [Fact]
    public void UnionKindOfIntroducedUnionIsTheDeclarationForm()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var introducedType = Introduce( compilation, CreateUnionBuilder( compilation ) );

        Assert.Equal( UnionKind.Declaration, introducedType.Facets.Union!.UnionKind );
    }

    /// <summary>
    /// Verifies that the cases of an introduced union are reported in the order in which they were added, each with
    /// the constructor that creates it.
    /// </summary>
    [Fact]
    public void CasesAreReportedInTheOrderInWhichTheyWereAdded()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var introducedType = Introduce( compilation, CreateUnionBuilder( compilation ) );

        var cases = introducedType.Facets.Union!.Cases;

        Assert.Equal( 2, cases.Count );
        Assert.Equal( SpecialType.Int32, cases[0].Type.SpecialType );
        Assert.Equal( SpecialType.String, cases[1].Type.SpecialType );
        Assert.Equal( 0, cases[0].Index );
        Assert.Equal( 1, cases[1].Index );

        // The creation member of a union declaration is the public constructor that takes the case as its only
        // parameter, which the advice registers without emitting it.
        Assert.All( cases, c => Assert.Equal( DeclarationKind.Constructor, c.CreationMember.DeclarationKind ) );
    }

    /// <summary>
    /// Verifies that the <c>Value</c> property of an introduced union is in the code model, which is where the
    /// facet reads it.
    /// </summary>
    [Fact]
    public void ValuePropertyIsInTheCodeModel()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var introducedType = Introduce( compilation, CreateUnionBuilder( compilation ) );

        var valueProperty = introducedType.Facets.Union!.ValueProperty;

        Assert.NotNull( valueProperty );
        Assert.Equal( "Value", valueProperty.Name );
        Assert.Equal( Accessibility.Public, valueProperty.Accessibility );
        Assert.Equal( SpecialType.Object, valueProperty.Type.SpecialType );
    }

    /// <summary>
    /// Verifies that a duplicate case is refused. The compiler reports the case types of a union as a set, so a
    /// second case of the same type could never be reported by the introduced type.
    /// </summary>
    [Fact]
    public void DuplicateCaseIsRefused()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreateUnionBuilder( compilation );

        Assert.Throws<ArgumentException>( () => builder.AddCase( compilation.Factory.GetSpecialType( SpecialType.Int32 ) ) );
        Assert.Throws<ArgumentException>( () => builder.AddCase( typeof(int) ) );
    }

    /// <summary>
    /// Verifies that the builder refuses a case once it is frozen.
    /// </summary>
    [Fact]
    public void FrozenBuilderRefusesACase()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreateUnionBuilder( compilation );
        builder.Freeze();

        Assert.Throws<InvalidOperationException>( () => builder.AddCase( compilation.Factory.GetSpecialType( SpecialType.Boolean ) ) );
    }

    /// <summary>
    /// Verifies that <see cref="INamedType.Facets"/> of a union builder throws, while
    /// <see cref="INamedType.IsUnion"/> answers without throwing. See section 5.1 of
    /// <c>Metalama.Framework/docs/introducing-types.md</c>.
    /// </summary>
    [Fact]
    public void FacetsOfUnionBuilderThrow()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = CreateUnionBuilder( compilation );

        Assert.True( builder.IsUnion );
        Assert.Throws<NotSupportedException>( () => builder.Facets );
    }
#else

    /// <summary>
    /// Verifies that introducing a union reports that the operation is not supported on the Roslyn variant whose
    /// Roslyn cannot emit a union declaration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The writer refuses the request instead of producing an ordinary struct, so an aspect never silently obtains a
    /// type other than the one it asked for. At design time the host that runs Metalama is the integrated
    /// development environment, which is why the message names it.
    /// </para>
    /// </remarks>
    [Fact]
    public void IntroducingAUnionIsRefusedOnTheLowerRoslynVariant()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var exception = Assert.Throws<InvalidOperationException>(
            () => new UnionBuilder( null!, compilation.GlobalNamespace, "IntroducedUnion" ) );

        Assert.Contains( "does not support the unions of C# 15", exception.Message, StringComparison.Ordinal );
    }
#endif
}
