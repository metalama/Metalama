// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Testing.UnitTesting;
using System;
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
/// Tests of <see cref="INamedType.IsClosed"/> and of <see cref="INamedTypeBuilder.IsClosed"/>, the code model
/// reader and writer of the <c>closed</c> modifier of C# 15.
/// See issues #1939 and #1950.
/// </summary>
public sealed class ClosedTypeTests : UnitTestClass
{
    /// <summary>
    /// Verifies that the property is <c>false</c> for every kind of type that is not closed, and that reading it
    /// reports no diagnostic. This test carries no condition, so it also pins the constant <c>false</c> that the
    /// Roslyn 5.0 variant compiles, whose Roslyn does not declare <c>ITypeSymbol.IsClosed</c>.
    /// </summary>
    [Fact]
    public void IsClosedIsFalseForTypesThatAreNotClosed()
    {
        const string code = """
                            abstract class AbstractClass;
                            class OrdinaryClass;
                            sealed class SealedClass;
                            static class StaticClass { }
                            record RecordClass;
                            struct Struct;
                            interface IInterface;
                            enum Enum { Value }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );

        Assert.All( compilation.Types, type => Assert.False( type.IsClosed, $"{type.Name} should not be closed." ) );
    }

    /// <summary>
    /// Verifies that the property is <c>false</c> for a type introduced by an aspect that does not set it, which is
    /// its default value, and that the builder and the introduced type agree.
    /// </summary>
    [Fact]
    public void IsClosedIsFalseForIntroducedType()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", TypeKind.Class );
        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        Assert.False( builder.IsClosed );
        Assert.False( compilation.Types.OfName( "IntroducedType" ).Single().IsClosed );
    }

#if ROSLYN_5_10_0_OR_GREATER && ALLOW_PREVIEW_LANG_VERSION

    // The writer refuses the closed modifier in a build that cannot emit it, so the tests of the writer are compiled
    // under the same condition as the setter of NamedTypeBuilder.IsClosed. The test below the #else covers the
    // builds that refuse it. Drop ALLOW_PREVIEW_LANG_VERSION from this condition, and from the condition of the
    // setter and of ModifierHelper, when issue #1936 brings a Roslyn that publishes the member without the
    // RSEXPERIMENTAL006 marker.

    /// <summary>
    /// Verifies that an aspect can introduce a closed class: the builder stores the value, the introduced type
    /// reports it, and <see cref="IMemberOrNamedType.IsAbstract"/> reports true, because a closed class is implicitly
    /// abstract. See issue #1950.
    /// </summary>
    [Fact]
    public void IsClosedIsTrueForIntroducedClosedClass()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", TypeKind.Class );
        builder.IsClosed = true;

        Assert.True( builder.IsClosed );
        Assert.True( builder.IsAbstract );

        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        var introducedType = compilation.Types.OfName( "IntroducedType" ).Single();

        Assert.True( introducedType.IsClosed );
        Assert.True( introducedType.IsAbstract );
    }

    /// <summary>
    /// Verifies that the setter refuses a type kind that is not a class, which is the restriction that the language
    /// states. A record class is a class here, because <see cref="TypeKind.Class"/> covers it.
    /// </summary>
    [Theory]
    [InlineData( TypeKind.Struct )]
    [InlineData( TypeKind.Interface )]
    public void IsClosedIsRejectedForTypeThatIsNotAClass( TypeKind typeKind )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", typeKind );

        Assert.Throws<InvalidOperationException>( () => builder.IsClosed = true );
    }

    /// <summary>
    /// Verifies that the setter refuses a sealed class, which the language forbids because a closed class is
    /// implicitly abstract.
    /// </summary>
    [Fact]
    public void IsClosedIsRejectedForSealedClass()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", TypeKind.Class )
        {
            IsSealed = true
        };

        Assert.Throws<InvalidOperationException>( () => builder.IsClosed = true );
    }

    /// <summary>
    /// Verifies that the setter refuses a static class, which the language forbids for the same reason.
    /// </summary>
    [Fact]
    public void IsClosedIsRejectedForStaticClass()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", TypeKind.Class )
        {
            IsStatic = true
        };

        Assert.Throws<InvalidOperationException>( () => builder.IsClosed = true );
    }

    /// <summary>
    /// Verifies that the setter of <see cref="IMemberOrNamedTypeBuilder.IsAbstract"/> refuses the value <c>false</c>
    /// on a closed type, because the language makes a closed class implicitly abstract.
    /// </summary>
    [Fact]
    public void IsAbstractIsRejectedForClosedClass()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", TypeKind.Class )
        {
            IsClosed = true
        };

        Assert.Throws<InvalidOperationException>( () => builder.IsAbstract = false );

        // Setting the property to the value that the type already has is allowed.
        builder.IsAbstract = true;

        Assert.True( builder.IsAbstract );
    }

#else

    /// <summary>
    /// Verifies that the setter refuses the closed modifier in a build whose Roslyn version does not offer C# 15,
    /// because such a build cannot emit the keyword. The restriction applies whatever the type kind, so this test
    /// also covers the kinds that the language forbids anyway.
    /// </summary>
    [Theory]
    [InlineData( TypeKind.Class )]
    [InlineData( TypeKind.Struct )]
    public void IsClosedIsRejectedWhenTheRoslynVersionDoesNotSupportIt( TypeKind typeKind )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", typeKind );

        Assert.Throws<InvalidOperationException>( () => builder.IsClosed = true );

        // The value false is accepted, because it requests nothing.
        builder.IsClosed = false;

        Assert.False( builder.IsClosed );
    }

#endif

#if ROSLYN_5_10_0_OR_GREATER && ALLOW_PREVIEW_LANG_VERSION

    // The closed modifier is a C# 15 feature, so only the preview language version of the latest Roslyn variant parses
    // it, and the engine reads ITypeSymbol.IsClosed only when ALLOW_PREVIEW_LANG_VERSION, the opt-in of
    // eng/RoslynPreview.props, is set, because the consumed Roslyn still marks that member with RSEXPERIMENTAL006.
    // The default build of the latest variant therefore answers false for a closed class by design, and a test that
    // did not require the opt-in would fail there. Drop ALLOW_PREVIEW_LANG_VERSION from this condition, and from the
    // condition of the reader in SourceNamedTypeImpl, when issue #1936 brings a Roslyn that publishes the member
    // without the marker.

#if NET7_0_OR_GREATER

    // We don't run these tests with old frameworks because the compiler emits CompilerFeatureRequiredAttribute on the
    // constructor of a closed class, and .NET Framework does not declare that type.

    private const string _closedTypeCode = """
                                           namespace System.Runtime.CompilerServices
                                           {
                                               // Stands for the attribute that the compiler emits on a closed class.
                                               // No target framework declares it yet, and the compiler reports CS0656
                                               // when it cannot find it.
                                               [AttributeUsage( AttributeTargets.Class )]
                                               public sealed class IsClosedTypeAttribute : Attribute;
                                           }

                                           closed class ClosedClass
                                           {
                                               public sealed class Case1 : ClosedClass;

                                               public sealed class Case2 : ClosedClass;
                                           }

                                           abstract class AbstractClass;
                                           """;

    /// <summary>
    /// Verifies the acceptance criterion of the story: an aspect can tell a closed class from an ordinary abstract
    /// class, which no other property of the code model allows, because Roslyn marks a closed class as abstract.
    /// </summary>
    [Fact]
    public void IsClosedIsTrueForClosedClassOnly()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateClosedTypeCompilation( testContext );

        var closedClass = compilation.Types.OfName( "ClosedClass" ).Single();
        var abstractClass = compilation.Types.OfName( "AbstractClass" ).Single();

        Assert.True( closedClass.IsClosed );
        Assert.False( abstractClass.IsClosed );

        // The two types are otherwise indistinguishable in the code model, which is why IsClosed is needed.
        Assert.True( closedClass.IsAbstract );
        Assert.True( abstractClass.IsAbstract );
        Assert.False( closedClass.IsSealed );
        Assert.False( abstractClass.IsSealed );

        // A nested type of a closed class is not itself closed.
        Assert.All( closedClass.Types, type => Assert.False( type.IsClosed ) );
    }

    /// <summary>
    /// Verifies the exhaustiveness rule that the documentation of <see cref="DerivedTypesOptions.DirectOnly"/>
    /// states: for a closed type declared in the current compilation, the direct enumeration of the derived types is
    /// the complete set of its subtypes, because the language requires every subtype to be declared in the same
    /// module.
    /// </summary>
    [Fact]
    public void DirectlyDerivedTypesOfClosedClassAreExhaustive()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateClosedTypeCompilation( testContext );

        var closedClass = compilation.Types.OfName( "ClosedClass" ).Single();

        var derivedTypes = compilation.GetDerivedTypes( closedClass, DerivedTypesOptions.DirectOnly )
            .Select( t => t.Name )
            .OrderBy( n => n )
            .ToArray();

        Assert.Equal( ["Case1", "Case2"], derivedTypes );
    }

    /// <summary>
    /// Creates a compilation of <see cref="_closedTypeCode"/>. The helpers of <see cref="TestContext"/> parse with
    /// <see cref="SupportedCSharpVersions.Latest"/>, which is the language version that Metalama allows a user project
    /// to use and which does not parse the <c>closed</c> modifier, so the syntax tree is parsed here with the preview
    /// language version instead.
    /// </summary>
    private static ICompilation CreateClosedTypeCompilation( TestContext testContext )
    {
        var parseOptions = SupportedCSharpVersions.DefaultParseOptions.WithLanguageVersion( LanguageVersion.Preview );

        var roslynCompilation = testContext.CreateEmptyCSharpCompilation( null )
            .AddSyntaxTrees( CSharpSyntaxTree.ParseText( _closedTypeCode, parseOptions, "closedTypes.cs" ) );

        Assert.Empty( roslynCompilation.GetDiagnostics().Where( d => d.Severity == DiagnosticSeverity.Error ) );

        return testContext.CreateCompilation( roslynCompilation );
    }

#endif
#endif
}
