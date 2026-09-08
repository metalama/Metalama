// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;
using TypeKind = Metalama.Framework.Code.TypeKind;
#if ROSLYN_5_10_0_OR_GREATER && ALLOW_PREVIEW_LANG_VERSION
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
#endif

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests of <see cref="INamedType.IsUnion"/>, <see cref="INamedType.IsUnionDeclaration"/> and
/// <see cref="INamedType.UnionCaseTypes"/>, the code model readers of the union of C# 15. See issue #1941.
/// </summary>
public sealed class UnionTypeTests : UnitTestClass
{
    /// <summary>
    /// Verifies that the three members report the value of an ordinary type for every kind of type that is not a
    /// union, and that reading them reports no diagnostic. This test carries no condition, so it also pins the
    /// constant answer that the Roslyn 5.0 variant compiles, whose Roslyn does not declare
    /// <c>ITypeSymbol.IsUnion</c>.
    /// </summary>
    [Fact]
    public void UnionMembersAreFalseAndEmptyForTypesThatAreNotUnions()
    {
        const string code = """
                            class OrdinaryClass;
                            struct OrdinaryStruct;
                            record RecordClass;
                            record struct RecordStruct;
                            interface IInterface;
                            enum Enum { Value }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );

        Assert.All(
            compilation.Types,
            type =>
            {
                Assert.False( type.IsUnion, $"{type.Name} should not be a union." );
                Assert.False( type.IsUnionDeclaration, $"{type.Name} should not be a union declaration." );
                Assert.Empty( type.UnionCaseTypes );
            } );
    }

    /// <summary>
    /// Verifies that the three members report the value of an ordinary type for a type introduced by an aspect,
    /// which is their value until an introduction story adds the writer, and that the builder and the introduced
    /// type agree.
    /// </summary>
    [Fact]
    public void UnionMembersAreFalseAndEmptyForIntroducedType()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", TypeKind.Class );
        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        Assert.False( builder.IsUnion );
        Assert.False( builder.IsUnionDeclaration );
        Assert.Empty( builder.UnionCaseTypes );

        var introducedType = compilation.Types.OfName( "IntroducedType" ).Single();

        Assert.False( introducedType.IsUnion );
        Assert.False( introducedType.IsUnionDeclaration );
        Assert.Empty( introducedType.UnionCaseTypes );
    }

#if ROSLYN_5_10_0_OR_GREATER && ALLOW_PREVIEW_LANG_VERSION

    // The union is a C# 15 feature, so only the preview language version of the latest Roslyn variant parses it, and
    // the engine reads ITypeSymbol.IsUnion only when ALLOW_PREVIEW_LANG_VERSION, the opt-in of
    // eng/RoslynPreview.props, is set, because the consumed Roslyn still marks that member with RSEXPERIMENTAL006.
    // The default build of the latest variant therefore answers false for a union by design, and a test that did not
    // require the opt-in would fail there. Drop ALLOW_PREVIEW_LANG_VERSION from this condition, and from the
    // condition of the readers in SourceNamedTypeImpl, when issue #1936 brings a Roslyn that publishes the member
    // without the marker.

#if NET7_0_OR_GREATER

    // We don't run these tests with old frameworks because the compiler emits CompilerFeatureRequiredAttribute on the
    // members of a union, and .NET Framework does not declare that type.

    private const string _unionCode = """
                                      using System;
                                      using System.Runtime.CompilerServices;

                                      namespace System.Runtime.CompilerServices
                                      {
                                          // These two stand for the types that the compiler requires of a union. No
                                          // target framework declares them yet, and the compiler reports CS0656 when
                                          // it cannot find them.
                                          [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct )]
                                          public sealed class UnionAttribute : Attribute;

                                          public interface IUnion;
                                      }

                                      union Shape( Circle, Rectangle );

                                      partial union PartialShape( Circle, Rectangle );

                                      partial union PartialShape;

                                      // The second authoring form: a type that carries the union attribute. The
                                      // language restrictions on the members of a union apply to the declaration form
                                      // only, which is why the code model tells the two apart.
                                      [Union]
                                      class AttributeUnion : IUnion
                                      {
                                          public AttributeUnion( Circle circle ) { this.Value = circle; }

                                          public object Value { get; }
                                      }

                                      record Circle( double Radius );

                                      record Rectangle( double Width, double Height );

                                      struct OrdinaryStruct;
                                      """;

    /// <summary>
    /// Verifies the acceptance criterion of the story: an aspect can tell a union from an ordinary struct, which no
    /// other property of the code model allows, because Roslyn reports a union declaration as a struct.
    /// </summary>
    [Fact]
    public void IsUnionIsTrueForBothAuthoringForms()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext );

        var shape = compilation.Types.OfName( "Shape" ).Single();
        var attributeUnion = compilation.Types.OfName( "AttributeUnion" ).Single();
        var ordinaryStruct = compilation.Types.OfName( "OrdinaryStruct" ).Single();

        Assert.True( shape.IsUnion );
        Assert.True( attributeUnion.IsUnion );
        Assert.False( ordinaryStruct.IsUnion );

        // The union declaration and the ordinary struct are otherwise indistinguishable in the code model, which is
        // why IsUnion is needed. No TypeKind value is added for a union.
        Assert.Equal( TypeKind.Struct, shape.TypeKind );
        Assert.Equal( TypeKind.Struct, ordinaryStruct.TypeKind );
        Assert.False( shape.IsRecord );
    }

    /// <summary>
    /// Verifies the acceptance criterion that an aspect can tell the two authoring forms apart. The restrictions of
    /// the language on the instance fields, the automatic properties and the field-like events of a union are
    /// checked by the compiler for a union declaration only.
    /// </summary>
    [Fact]
    public void IsUnionDeclarationTellsTheTwoAuthoringFormsApart()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext );

        Assert.True( compilation.Types.OfName( "Shape" ).Single().IsUnionDeclaration );
        Assert.False( compilation.Types.OfName( "AttributeUnion" ).Single().IsUnionDeclaration );
        Assert.False( compilation.Types.OfName( "OrdinaryStruct" ).Single().IsUnionDeclaration );
    }

    /// <summary>
    /// Verifies the acceptance criterion that an aspect can enumerate the case types of a union, in the order in
    /// which the union header declares them.
    /// </summary>
    [Fact]
    public void UnionCaseTypesEnumeratesTheDeclaredCases()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext );

        var shape = compilation.Types.OfName( "Shape" ).Single();

        Assert.Equal( ["Circle", "Rectangle"], shape.UnionCaseTypes.Select( t => ((INamedType) t).Name ).ToArray() );

        // The case types are pre-existing types named in the union header. None of them is a union itself.
        Assert.All( shape.UnionCaseTypes, t => Assert.False( ((INamedType) t).IsUnion ) );

        // The consumed Roslyn does not model the case types of the attribute form, and a union read from a referenced
        // assembly has no union header, so the list is empty for both.
        Assert.Empty( compilation.Types.OfName( "AttributeUnion" ).Single().UnionCaseTypes );
        Assert.Empty( compilation.Types.OfName( "OrdinaryStruct" ).Single().UnionCaseTypes );
    }

    /// <summary>
    /// Verifies that the case list of a partial union is read from the part that declares it, whichever part is the
    /// primary declaration.
    /// </summary>
    [Fact]
    public void UnionCaseTypesOfPartialUnionAreReadFromThePartThatDeclaresThem()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext );

        var partialShape = compilation.Types.OfName( "PartialShape" ).Single();

        Assert.True( partialShape.IsUnion );
        Assert.Equal( ["Circle", "Rectangle"], partialShape.UnionCaseTypes.Select( t => ((INamedType) t).Name ).ToArray() );
    }

    /// <summary>
    /// Verifies the acceptance criterion that <see cref="INamedType.IsPartial"/> is true for a partial union. The
    /// property answers from the modifiers of the primary declaration only when the syntax kind of that declaration
    /// is one that <c>SyntaxKindExtensions.IsTypeDeclaration</c> admits, and the union kind was missing from that
    /// list, so <c>LAMA0048</c> was reported although the type is partial.
    /// </summary>
    [Fact]
    public void IsPartialIsTrueForPartialUnion()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext );

        Assert.True( compilation.Types.OfName( "PartialShape" ).Single().IsPartial );
        Assert.False( compilation.Types.OfName( "Shape" ).Single().IsPartial );
    }

    /// <summary>
    /// Creates a compilation of <see cref="_unionCode"/>. The helpers of <see cref="TestContext"/> parse with
    /// <see cref="SupportedCSharpVersions.Latest"/>, which is the language version that Metalama allows a user
    /// project to use and which does not parse a union declaration, so the syntax tree is parsed here with the
    /// preview language version instead.
    /// </summary>
    private static ICompilation CreateUnionCompilation( TestContext testContext )
    {
        var parseOptions = SupportedCSharpVersions.DefaultParseOptions.WithLanguageVersion( LanguageVersion.Preview );

        var roslynCompilation = testContext.CreateEmptyCSharpCompilation( null )
            .AddSyntaxTrees( CSharpSyntaxTree.ParseText( _unionCode, parseOptions, "unions.cs" ) );

        Assert.Empty( roslynCompilation.GetDiagnostics().Where( d => d.Severity == DiagnosticSeverity.Error ) );

        return testContext.CreateCompilation( roslynCompilation );
    }

#endif
#endif
}
