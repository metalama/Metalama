// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;
using TypeKind = Metalama.Framework.Code.TypeKind;
#if ROSLYN_5_11_0_OR_GREATER
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.IO;
#endif

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests of <see cref="INamedType.IsUnion"/> and of <see cref="IUnionFacet"/>, the code model readers of the union of
/// C# 15. See issue #1941 and the design document <c>Metalama.Framework/docs/future/type-facets.md</c>.
/// </summary>
public sealed class UnionTypeTests : UnitTestClass
{
    /// <summary>
    /// Verifies that a type that is not a union reports <see cref="INamedType.IsUnion"/> as <c>false</c> and has no
    /// union facet, and that reading the two reports no diagnostic. This test carries no condition, so it also pins
    /// the constant answer that the Roslyn 5.0 variant compiles, whose Roslyn does not declare
    /// <c>ITypeSymbol.IsUnion</c>.
    /// </summary>
    [Fact]
    public void UnionMembersAreEmptyForTypesThatAreNotUnions()
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
                Assert.Null( type.Facets.Union );
            } );
    }

    /// <summary>
    /// Verifies that a class introduced by an aspect is not a union and has no union facet, and that the builder
    /// refuses to report a facet at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The builder throws from <c>Facets</c>, which is section 5.1 of
    /// <c>Metalama.Framework/docs/future/introducing-types.md</c>, while <c>IsUnion</c> answers without throwing.
    /// The introduced type reports the facet of its kind, and a class has none.
    /// </para>
    /// </remarks>
    [Fact]
    public void UnionMembersAreEmptyForIntroducedClass()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

        var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", TypeKind.Class );

        Assert.False( builder.IsUnion );
        Assert.Throws<NotSupportedException>( () => builder.Facets );

        builder.Freeze();
        compilation.AddTransformation( builder.CreateTransformation() );

        var introducedType = compilation.Types.OfName( "IntroducedType" ).Single();

        Assert.False( introducedType.IsUnion );
        Assert.Null( introducedType.Facets.Union );
    }

#if !ROSLYN_5_11_0_OR_GREATER

    /// <summary>
    /// Verifies that the attribute form of a union reports <see cref="INamedType.IsUnion"/> as <c>false</c>, has no
    /// union facet and reports no diagnostic on the Roslyn 5.0 variant, whose Roslyn has no notion of a union at all.
    /// That variant is the one that this test pins, because it is the only one on which the attribute form is an
    /// ordinary class. The declaration form cannot be pinned in the same way, because that Roslyn does not parse it.
    /// </summary>
    [Fact]
    public void AttributeUnionIsNotAUnionOnTheLowerRoslynVariant()
    {
        const string code = """
                            using System;
                            using System.Runtime.CompilerServices;

                            namespace System.Runtime.CompilerServices
                            {
                                [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct )]
                                public sealed class UnionAttribute : Attribute;

                                public interface IUnion;
                            }

                            [Union]
                            class AttributeUnion : IUnion
                            {
                                public AttributeUnion( int value ) { this.Value = value; }

                                public object Value { get; }
                            }
                            """;

        using var testContext = this.CreateTestContext();

        // The helper reports an exception when the compilation has an error, so creating it pins that reading the
        // code model of this type reports no diagnostic.
        var compilation = testContext.CreateCompilation( code );

        var attributeUnion = compilation.Types.OfName( "AttributeUnion" ).Single();

        Assert.False( attributeUnion.IsUnion );
        Assert.Null( attributeUnion.Facets.Union );
        Assert.Empty( attributeUnion.Facets );
    }

#endif

#if ROSLYN_5_11_0_OR_GREATER

    // The union is a C# 15 feature, so only the latest Roslyn variant parses it and only that variant reads
    // ITypeSymbol.IsUnion, under the same condition as the readers in SourceNamedTypeImpl. Roslyn 5.11 declares C# 15
    // and publishes the member without the RSEXPERIMENTAL006 marker, so the condition names the variant symbol alone.

#if NET7_0_OR_GREATER

    // We don't run these tests with old frameworks because the compiler emits CompilerFeatureRequiredAttribute on the
    // members of a union, and .NET Framework does not declare that type.

    /// <summary>
    /// The declarations that the compiler requires of a union. No target framework declares them yet, and the
    /// compiler reports CS0656 when it cannot find them, so every compilation of this class declares them.
    /// </summary>
    private const string _unionSupportCode = """
                                             using System;

                                             namespace System.Runtime.CompilerServices
                                             {
                                                 [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct )]
                                                 public sealed class UnionAttribute : Attribute;

                                                 public interface IUnion;
                                             }
                                             """;

    private const string _unionCode = """
                                      using System.Runtime.CompilerServices;

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

                                          public AttributeUnion( Rectangle rectangle ) { this.Value = rectangle; }

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
    /// Verifies that a union has the union facet, that the facet reports the type it belongs to, and that the facet
    /// collection contains exactly that facet.
    /// </summary>
    [Fact]
    public void UnionHasTheUnionFacet()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext );

        var shape = compilation.Types.OfName( "Shape" ).Single();

        var facet = shape.Facets.Union;

        Assert.NotNull( facet );
        Assert.Equal( TypeFacetKind.Union, facet.FacetKind );
        Assert.Same( shape, facet.Type );

        // The count is read into a local because the invariant is that it agrees with the typed properties, which is
        // a different statement from the single element of the enumeration asserted below.
        var facetCount = shape.Facets.Count;

        Assert.Equal( 1, facetCount );
        Assert.Same( facet, Assert.Single( shape.Facets ) );
        Assert.Null( shape.Facets.Delegate );

        Assert.Null( compilation.Types.OfName( "OrdinaryStruct" ).Single().Facets.Union );
    }

    /// <summary>
    /// Verifies the acceptance criterion that an aspect can tell the two authoring forms apart. The restrictions of
    /// the language on the instance fields, the automatic properties and the field-like events of a union are
    /// checked by the compiler for a union declaration only.
    /// </summary>
    [Fact]
    public void UnionKindTellsTheTwoAuthoringFormsApart()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext );

        Assert.Equal( UnionKind.Declaration, compilation.Types.OfName( "Shape" ).Single().Facets.Union!.UnionKind );
        Assert.Equal( UnionKind.Declaration, compilation.Types.OfName( "PartialShape" ).Single().Facets.Union!.UnionKind );
        Assert.Equal( UnionKind.Attribute, compilation.Types.OfName( "AttributeUnion" ).Single().Facets.Union!.UnionKind );
    }

    /// <summary>
    /// Verifies the acceptance criterion that an aspect can enumerate the cases of a union, in the order in which the
    /// union header declares them, and that each case names the constructor that creates a value of it.
    /// </summary>
    [Fact]
    public void CasesOfUnionDeclarationAreTheDeclaredCases()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext );

        var shape = compilation.Types.OfName( "Shape" ).Single();
        var cases = shape.Facets.Union!.Cases;

        Assert.Equal( ["Circle", "Rectangle"], cases.SelectAsArray( c => ((INamedType) c.Type).Name ) );
        Assert.Equal( [0, 1], cases.SelectAsArray( c => c.Index ) );

        // The compiler synthesizes one public constructor per case type, and that constructor is the creation member
        // of the case.
        Assert.All(
            cases,
            unionCase =>
            {
                var creationMember = Assert.IsAssignableFrom<IConstructor>( unionCase.CreationMember );

                Assert.Same( shape, creationMember.DeclaringType );
                Assert.Equal( unionCase.Type, creationMember.Parameters.Single().Type );
            } );

        // The case types are pre-existing types named in the union header. None of them is a union itself.
        Assert.All( cases, unionCase => Assert.False( ((INamedType) unionCase.Type).IsUnion ) );
    }

    /// <summary>
    /// Verifies that the cases of a partial union are reported although only one part of the union carries the case
    /// list, which is what the language requires.
    /// </summary>
    [Fact]
    public void CasesOfPartialUnionAreReported()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext );

        var partialShape = compilation.Types.OfName( "PartialShape" ).Single();

        Assert.True( partialShape.IsUnion );
        Assert.Equal( ["Circle", "Rectangle"], partialShape.Facets.Union!.Cases.SelectAsArray( c => ((INamedType) c.Type).Name ) );
    }

    /// <summary>
    /// Verifies that the cases of the attribute form of a union are the public single-parameter constructors of the
    /// type, which is the derivation that the language applies to that form.
    /// </summary>
    [Fact]
    public void CasesOfAttributeUnionAreItsPublicSingleParameterConstructors()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext );

        var attributeUnion = compilation.Types.OfName( "AttributeUnion" ).Single();
        var cases = attributeUnion.Facets.Union!.Cases;

        Assert.Equal( ["Circle", "Rectangle"], cases.SelectAsArray( c => ((INamedType) c.Type).Name ) );
        Assert.Equal( [0, 1], cases.SelectAsArray( c => c.Index ) );
        Assert.All( cases, unionCase => Assert.IsAssignableFrom<IConstructor>( unionCase.CreationMember ) );
    }

    /// <summary>
    /// Verifies that the cases of a union whose creation members are declared on a union member provider interface
    /// are read from that interface, and that the creation member of such a case is a method and not a constructor.
    /// </summary>
    [Fact]
    public void CasesOfUnionWithMemberProviderAreItsCreateMethods()
    {
        // The compiler accepts a union member provider only in this shape: the nested interface is named
        // IUnionMembers, declares the Value property and one static abstract Create method per case, and the union
        // implements it. A union that declares the Create methods without the interface is reported as having no
        // creation member, which is CS9385.
        const string code = """
                            using System.Runtime.CompilerServices;

                            [Union]
                            class ProviderUnion : IUnion, ProviderUnion.IUnionMembers
                            {
                                private ProviderUnion( object value ) { this.Value = value; }

                                public object Value { get; }

                                public static ProviderUnion Create( Circle circle ) => new ProviderUnion( circle );

                                public static ProviderUnion Create( Rectangle rectangle ) => new ProviderUnion( rectangle );

                                public interface IUnionMembers
                                {
                                    object Value { get; }

                                    static abstract ProviderUnion Create( Circle circle );

                                    static abstract ProviderUnion Create( Rectangle rectangle );
                                }
                            }

                            record Circle( double Radius );

                            record Rectangle( double Width, double Height );
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext, code );

        var providerUnion = compilation.Types.OfName( "ProviderUnion" ).Single();

        Assert.True( providerUnion.IsUnion );

        var cases = providerUnion.Facets.Union!.Cases;

        Assert.Equal( ["Circle", "Rectangle"], cases.SelectAsArray( c => ((INamedType) c.Type).Name ) );

        var memberProviderInterface = providerUnion.Types.OfName( "IUnionMembers" ).Single();

        Assert.All(
            cases,
            unionCase =>
            {
                var creationMember = Assert.IsAssignableFrom<IMethod>( unionCase.CreationMember );

                Assert.Equal( "Create", creationMember.Name );
                Assert.True( creationMember.IsStatic );

                // The creation member is the declaration of the member provider interface and not the implementation
                // that the union declares, because the interface is what the compiler reads.
                Assert.Equal( memberProviderInterface, creationMember.DeclaringType );
            } );

        // The Value property is resolved on the member provider interface for the same reason.
        Assert.Equal( memberProviderInterface, providerUnion.Facets.Union!.ValueProperty?.DeclaringType );
    }

    /// <summary>
    /// Verifies that the cases of a union whose member provider interface extends another interface include the cases
    /// that the extended interface declares. The compiler reads the member provider interface and then the interfaces
    /// it inherits, so a derivation that read the <c>Create</c> methods of the union alone would drop them.
    /// </summary>
    [Fact]
    public void CasesOfUnionWithMemberProviderIncludeThoseOfTheInheritedInterfaces()
    {
        const string code = """
                            using System.Runtime.CompilerServices;

                            [Union]
                            class InheritedProviderUnion : IUnion, InheritedProviderUnion.IUnionMembers
                            {
                                private InheritedProviderUnion( object value ) { this.Value = value; }

                                public object Value { get; }

                                public static InheritedProviderUnion Create( Circle circle ) => new InheritedProviderUnion( circle );

                                public static InheritedProviderUnion Create( Rectangle rectangle ) => new InheritedProviderUnion( rectangle );

                                public interface IUnionMembersBase
                                {
                                    static abstract InheritedProviderUnion Create( Rectangle rectangle );
                                }

                                public interface IUnionMembers : IUnionMembersBase
                                {
                                    object Value { get; }

                                    static abstract InheritedProviderUnion Create( Circle circle );
                                }
                            }

                            record Circle( double Radius );

                            record Rectangle( double Width, double Height );
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext, code );

        var inheritedProviderUnion = compilation.Types.OfName( "InheritedProviderUnion" ).Single();
        var cases = inheritedProviderUnion.Facets.Union!.Cases;

        // The interface itself is read first and the interfaces it inherits follow, which is the order of the cases.
        Assert.Equal( ["Circle", "Rectangle"], cases.SelectAsArray( c => ((INamedType) c.Type).Name ) );
        Assert.Equal( [0, 1], cases.SelectAsArray( c => c.Index ) );

        Assert.Equal( "IUnionMembers", cases[0].CreationMember.DeclaringType.Name );
        Assert.Equal( "IUnionMembersBase", cases[1].CreationMember.DeclaringType.Name );
    }

    /// <summary>
    /// Verifies that a nested interface named <c>IUnionMembers</c> that the union does not implement is not a member
    /// provider, so that the creation members of the union are its constructors. The name alone does not make a
    /// member provider, which is the rule the compiler applies.
    /// </summary>
    [Fact]
    public void NestedInterfaceThatTheUnionDoesNotImplementIsNotAMemberProvider()
    {
        const string code = """
                            using System.Runtime.CompilerServices;

                            [Union]
                            class NotAProviderUnion : IUnion
                            {
                                public NotAProviderUnion( Circle circle ) { this.Value = circle; }

                                public object Value { get; }

                                public interface IUnionMembers
                                {
                                    static abstract NotAProviderUnion Create( Rectangle rectangle );
                                }
                            }

                            record Circle( double Radius );

                            record Rectangle( double Width, double Height );
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext, code );

        var cases = compilation.Types.OfName( "NotAProviderUnion" ).Single().Facets.Union!.Cases;

        Assert.Equal( ["Circle"], cases.SelectAsArray( c => ((INamedType) c.Type).Name ) );
        Assert.IsAssignableFrom<IConstructor>( Assert.Single( cases ).CreationMember );
    }

    /// <summary>
    /// Verifies that a type occurs once in the case list although two constructors take it, and that a constructor
    /// whose parameter is passed by <c>ref</c> is not a creation member. The compiler collects the case types in a
    /// set and admits a parameter passed by value or by <c>in</c> only.
    /// </summary>
    [Fact]
    public void CasesAreTheDeduplicatedTypesOfTheSuitableCreationMembers()
    {
        const string code = """
                            using System.Runtime.CompilerServices;

                            [Union]
                            class OverloadedUnion : IUnion
                            {
                                public OverloadedUnion( Circle circle ) { this.Value = circle; }

                                public OverloadedUnion( in Circle circle ) { this.Value = circle; }

                                public OverloadedUnion( ref Rectangle rectangle ) { this.Value = rectangle; }

                                public object Value { get; }
                            }

                            record Circle( double Radius );

                            record Rectangle( double Width, double Height );
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext, code );

        var overloadedUnion = compilation.Types.OfName( "OverloadedUnion" ).Single();
        var cases = overloadedUnion.Facets.Union!.Cases;

        var unionCase = Assert.Single( cases );

        Assert.Equal( "Circle", ((INamedType) unionCase.Type).Name );
        Assert.Equal( 0, unionCase.Index );

        // The creation member of the case is the first of the two constructors that take the type of the case.
        Assert.Equal( Code.RefKind.None, ((IConstructor) unionCase.CreationMember).Parameters[0].RefKind );
    }

    /// <summary>
    /// Verifies that the <c>Value</c> property of a union that inherits it from a base type is reported. The compiler
    /// looks the property up in the union and then in its base types, so a lookup restricted to the union itself
    /// would report none for a valid union.
    /// </summary>
    [Fact]
    public void ValuePropertyIsResolvedInTheBaseTypes()
    {
        const string code = """
                            using System.Runtime.CompilerServices;

                            class UnionBase
                            {
                                public object Value { get; protected set; }
                            }

                            [Union]
                            class InheritedValueUnion : UnionBase, IUnion
                            {
                                public InheritedValueUnion( Circle circle ) { this.Value = circle; }
                            }

                            record Circle( double Radius );
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext, code );

        var inheritedValueUnion = compilation.Types.OfName( "InheritedValueUnion" ).Single();
        var valueProperty = inheritedValueUnion.Facets.Union!.ValueProperty;

        Assert.NotNull( valueProperty );
        Assert.Equal( "Value", valueProperty.Name );
        Assert.Equal( "UnionBase", valueProperty.DeclaringType.Name );
    }

    /// <summary>
    /// Verifies that the facet names the <c>Value</c> property, which the compiler synthesizes for a union
    /// declaration and which the language requires the attribute form to declare.
    /// </summary>
    [Fact]
    public void ValuePropertyIsReported()
    {
        using var testContext = this.CreateTestContext();
        var compilation = CreateUnionCompilation( testContext );

        foreach ( var typeName in new[] { "Shape", "PartialShape", "AttributeUnion" } )
        {
            var type = compilation.Types.OfName( typeName ).Single();
            var valueProperty = type.Facets.Union!.ValueProperty;

            Assert.NotNull( valueProperty );
            Assert.Equal( "Value", valueProperty.Name );
            Assert.Same( type, valueProperty.DeclaringType );
            Assert.Equal( Code.SpecialType.Object, valueProperty.Type.SpecialType );
        }
    }

    /// <summary>
    /// Verifies that a union read from a compiled assembly is reported as <see cref="UnionKind.Attribute"/>, and that
    /// its cases and its <c>Value</c> property are reported. The compiled form of every union carries the union
    /// attribute and does not record whether the source used the <c>union</c> keyword, so the attribute form is what
    /// the compiled shape has.
    /// </summary>
    [Fact]
    public void UnionReadFromCompiledAssemblyIsReportedAsTheAttributeForm()
    {
        using var testContext = this.CreateTestContext();

        var parseOptions = SupportedCSharpVersions.DefaultParseOptions;

        var referencedCompilation = testContext.CreateEmptyCSharpCompilation( "UnionDependency" )
            .AddSyntaxTrees(
                CSharpSyntaxTree.ParseText( _unionSupportCode, parseOptions, "support.cs" ),
                CSharpSyntaxTree.ParseText( _unionCode, parseOptions, "unions.cs" ) );

        using var peStream = new MemoryStream();
        var emitResult = referencedCompilation.Emit( peStream );

        Assert.True( emitResult.Success, string.Join( "\n", emitResult.Diagnostics ) );

        var reference = MetadataReference.CreateFromImage( ImmutableArray.Create( peStream.ToArray() ) );

        var roslynCompilation = testContext.CreateEmptyCSharpCompilation( null ).AddReferences( reference );

        Assert.Empty( roslynCompilation.GetDiagnostics().Where( d => d.Severity == DiagnosticSeverity.Error ) );

        var compilation = testContext.CreateCompilationModel( roslynCompilation );
        var shape = compilation.Factory.GetTypeByReflectionName( "Shape" );

        Assert.True( shape.DeclaringAssembly.IsExternal );
        Assert.True( shape.IsUnion );

        var facet = shape.Facets.Union;

        Assert.NotNull( facet );
        Assert.Equal( UnionKind.Attribute, facet.UnionKind );
        Assert.Equal( ["Circle", "Rectangle"], facet.Cases.SelectAsArray( c => ((INamedType) c.Type).Name ) );
        Assert.Equal( "Value", facet.ValueProperty?.Name );
    }

    /// <summary>
    /// Verifies the acceptance criterion that <see cref="IMemberOrNamedType.IsPartial"/> is true for a partial union. The
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
    /// Creates a compilation of <paramref name="code"/>, which defaults to <see cref="_unionCode"/>, together with
    /// <see cref="_unionSupportCode"/>. The syntax trees are parsed with
    /// <see cref="SupportedCSharpVersions.DefaultParseOptions"/>, whose language version is
    /// <see cref="SupportedCSharpVersions.Latest"/>. That version is C# 15 in this variant, which parses a union
    /// declaration, so no override is needed. Issue #2005 removed the preview override that stood here while C# 15
    /// was reached through <see cref="LanguageVersion.Preview"/>.
    /// </summary>
    private static ICompilation CreateUnionCompilation( TestContext testContext, string? code = null )
    {
        var parseOptions = SupportedCSharpVersions.DefaultParseOptions;

        var roslynCompilation = testContext.CreateEmptyCSharpCompilation( null )
            .AddSyntaxTrees(
                CSharpSyntaxTree.ParseText( _unionSupportCode, parseOptions, "support.cs" ),
                CSharpSyntaxTree.ParseText( code ?? _unionCode, parseOptions, "unions.cs" ) );

        Assert.Empty( roslynCompilation.GetDiagnostics().Where( d => d.Severity == DiagnosticSeverity.Error ) );

        return testContext.CreateCompilation( roslynCompilation );
    }

#endif
#endif
}
