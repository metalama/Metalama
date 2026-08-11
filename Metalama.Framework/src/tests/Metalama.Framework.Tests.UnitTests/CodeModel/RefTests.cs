// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

// Several tests below declare the type of a local explicitly, because what they assert is the static type of the
// expression and not merely the run-time type of the value it produces.
#pragma warning disable IDE0007 // Use 'var' instead of explicit type

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// The tests of <see cref="IRef.ToDurable"/> and <see cref="Code.RefExtensions.ToDurableRef{T}"/>, run once per kind of
/// durable reference by each derived class.
/// </summary>
/// <remarks>
/// <para>
/// What a durable reference is depends on the scope of the project: a batch compilation holds the reference it was made
/// from, and every other scope holds an identifier (see <see cref="IDurableRefFactory"/>). Every property asserted here
/// has to hold in each of them, because a call site asks for a durable reference without knowing which it will get, so
/// the tests are written once and the derived classes vary only the kind.
/// </para>
/// <para>
/// <see cref="SerializableRefResolutionTests"/> holds the tests that resolve an identifier directly. They exercise the
/// same resolution code, but they do not go through <see cref="IDurableRefFactory"/> at all, so running them three
/// times would run identical code three times.
/// </para>
/// </remarks>
public abstract class RefTests : UnitTestClass
{
    /// <summary>
    /// Gets the kind of durable reference that the tests of the current class run with.
    /// </summary>
    protected abstract DurableRefKind DurableRefKind { get; }

    /// <summary>
    /// Applies <see cref="DurableRefKind"/> to every test context of the class.
    /// </summary>
    /// <remarks>
    /// This overrides <c>CreateTestContextCore</c> rather than <c>CreateDefaultTestContextOptions</c>, because the
    /// latter is consulted only when the test passes no options of its own.
    /// </remarks>
    protected override TestContext CreateTestContextCore( TestContextOptions contextOptions, IAdditionalServiceCollection services )
        => base.CreateTestContextCore( contextOptions with { DurableRefKind = this.DurableRefKind }, services );

    /// <summary>
    /// <see cref="IRef{T}.ToDurable"/> is public, and it returns the strongly-typed <see cref="IDurableRef{T}"/> so that
    /// an API that must not retain a compilation can require durability in its signature rather than document it
    /// (issue #1806).
    /// </summary>
    [Fact]
    public void ToDurableReturnsATypedDurableRef()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class C { }" );

        var fullRef = compilation.Types.OfName( "C" ).Single().ToRef();

        Assert.False( fullRef.IsDurable );

        // The static type of the expression, not merely its run-time type, is what this test is about.
        IDurableRef<INamedType> durableRef = fullRef.ToDurable();

        Assert.True( durableRef.IsDurable );
        Assert.Same( compilation.Types.OfName( "C" ).Single(), durableRef.GetTarget( compilation ) );
    }

    /// <summary>
    /// Making a durable reference durable again returns the same instance rather than allocating an equivalent one.
    /// </summary>
    [Fact]
    public void ToDurableOnADurableRefReturnsTheSameInstance()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class C { }" );

        var durableRef = compilation.Types.OfName( "C" ).Single().ToRef().ToDurable();

        Assert.Same( durableRef, durableRef.ToDurable() );
    }

    /// <summary>
    /// The non-generic <see cref="IRef.ToDurable"/> returns an <see cref="IDurableRef"/>, so that a caller holding a
    /// weakly-typed reference can make it durable without knowing its type argument.
    /// </summary>
    [Fact]
    public void NonGenericToDurableReturnsADurableRef()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class C { }" );

        IRef weaklyTyped = compilation.Types.OfName( "C" ).Single().ToRef();

        IDurableRef durableRef = weaklyTyped.ToDurable();

        Assert.True( durableRef.IsDurable );
    }

    /// <summary>
    /// An attribute has no serializable identifier of its own, so its reference cannot be made durable and says so
    /// rather than returning something that would fail later.
    /// </summary>
    [Fact]
    public void ToDurableOnAnAttributeRefThrows()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "using System; [Obsolete] class C { }" );

        var attribute = compilation.Types.OfName( "C" ).Single().Attributes.Single();

        Assert.Throws<NotSupportedException>( () => attribute.ToRef().ToDurable() );
    }

    /// <summary>
    /// <see cref="Code.RefExtensions.ToDurableRef{T}"/> returns the strongly-typed <see cref="IDurableRef{T}"/> for the
    /// static type of its argument, so that a field declared as <see cref="IDurableRef{T}"/> can be assigned from a
    /// declaration without an intermediate reference and without a cast.
    /// </summary>
    [Fact]
    public void ToDurableRefReturnsATypedDurableRef()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class C { }" );

        var type = compilation.Types.OfName( "C" ).Single();

        // The static type of the expression, not merely its run-time type, is what this test is about.
        IDurableRef<INamedType> durableRef = type.ToDurableRef();

        Assert.True( durableRef.IsDurable );
        Assert.Same( type, durableRef.GetTarget( compilation ) );
    }

    /// <summary>
    /// Verifies that <see cref="Code.RefExtensions.ToDurableRef{T}"/> produces the same reference as
    /// <c>ToRef().ToDurable()</c> for every kind of declaration whose identifier is computed by a dedicated code path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Code.RefExtensions.ToDurableRef{T}"/> computes the identifier from the declaration instead of from the
    /// symbol of an intermediate <c>IFullRef</c>, so the two routes go through different code. A parameter, a return
    /// parameter and a type parameter are included because their identifiers are built from the identifier of the
    /// containing declaration rather than by the general case.
    /// </para>
    /// <para>
    /// This is also what holds the two kinds of durable reference to the same identifier. A named type is a
    /// declaration, so a reference that answered <see cref="IRef.ToSerializableId"/> from the declaration alone would
    /// lose the type arguments and the nullable annotation, which is the defect reported as issue #1797. The failure is
    /// silent everywhere else, because such a reference still resolves to a usable type.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( "Type" )]
    [InlineData( "Method" )]
    [InlineData( "Parameter" )]
    [InlineData( "ReturnParameter" )]
    [InlineData( "TypeParameter" )]
    [InlineData( "Field" )]
    public void ToDurableRefIsEquivalentToToRefThenToDurable( string kind )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class C<T> { int _f; int M( string p ) => 0; }" );

        var type = compilation.Types.OfName( "C" ).Single();
        var method = type.Methods.OfName( "M" ).Single();

        IDeclaration declaration = kind switch
        {
            "Type" => type,
            "Method" => method,
            "Parameter" => method.Parameters[0],
            "ReturnParameter" => method.ReturnParameter,
            "TypeParameter" => type.TypeParameters[0],
            "Field" => type.Fields.OfName( "_f" ).Single(),
            _ => throw new AssertionFailedException( $"Unknown kind '{kind}'." )
        };

        var throughRef = declaration.ToRef().ToDurable();
        var direct = declaration.ToDurableRef();

        Assert.Equal( throughRef.ToSerializableId(), direct.ToSerializableId() );
        Assert.True( direct.Equals( throughRef, RefComparison.Default ) );
        Assert.Same( declaration, direct.GetTarget( compilation ) );
    }

    /// <summary>
    /// Verifies that a durable reference produces the identifier that an identifier-based one would have carried,
    /// whatever the kind of the project.
    /// </summary>
    /// <remarks>
    /// This is the property the whole design rests on: a reference that holds the compilation still has to be written
    /// to a transitive manifest that another project, built in another scope, reads. A divergence here would not fail
    /// anywhere near its cause, because it would surface as a reference that resolves to the wrong declaration in the
    /// consuming project. See issue #1811.
    /// </remarks>
    [Theory]
    [InlineData( "Plain" )]
    [InlineData( "Generic" )]
    [InlineData( "Nested" )]
    [InlineData( "Constructed" )]
    [InlineData( "External" )]
    public void DurableRefCarriesTheSameIdentifierWhateverItsKind( string kind )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( RefTestFixtures.GenericTypesCode );

        var fullRef = (IFullRef<INamedType>) RefTestFixtures.GetTestType( compilation, kind ).ToRef();

        var expected = (IDurableRefImpl) SerializableDurableRefFactory.Instance.FromFullRef( fullRef );
        var actual = (IDurableRefImpl) fullRef.ToDurable();

        Assert.Equal( expected.Id, actual.Id );
        Assert.Equal( expected.ToSerializableId(), actual.ToSerializableId() );
    }

    /// <summary>
    /// A type that is not a declaration, such as an array type, has no declaration identifier, so
    /// <see cref="Code.RefExtensions.ToDurableRef{T}"/> identifies it by its <see cref="SerializableTypeId"/>.
    /// </summary>
    /// <remarks>
    /// <c>ToRef().ToDurable()</c> reaches the same identifier by a different route: the symbol overload of
    /// <c>GetSerializableId</c> returns the type identifier wrapped in a <see cref="SerializableDeclarationId"/> for
    /// these type kinds. The two references are therefore equal, and only the object that carries the identifier
    /// differs.
    /// </remarks>
    [Fact]
    public void ToDurableRefOnATypeThatIsNotADeclaration()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var arrayType = compilation.Factory.GetTypeByReflectionType( typeof(string[]) );

        var durableRef = arrayType.ToDurableRef();

        Assert.True( durableRef.IsDurable );
        Assert.Equal( arrayType.ToDisplayString(), durableRef.GetTarget( compilation ).ToDisplayString() );

        Assert.True( durableRef.Equals( arrayType.ToRef().ToDurable(), RefComparison.Default ) );
    }

    /// <summary>
    /// A constructed generic type keeps its type arguments, because it is identified by its
    /// <see cref="SerializableTypeId"/> rather than by a declaration identifier.
    /// </summary>
    /// <remarks>
    /// This is the counterpart of <see cref="DurableRefToConstructedGenericTypeKeepsTheTypeArguments"/>: both routes to
    /// a durable reference preserve the type arguments, so they agree.
    /// </remarks>
    [Fact]
    public void ToDurableRefToConstructedGenericTypeKeepsTheTypeArguments()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( RefTestFixtures.GenericTypesCode );

        var type = RefTestFixtures.GetTestType( compilation, "Constructed" );
        Assert.Equal( "Generic<int>", type.ToDisplayString() );

        Assert.Equal( "Generic<int>", type.ToDurableRef().GetTarget( compilation ).ToDisplayString() );
    }

    /// <summary>
    /// An attribute has no serializable identifier of its own, so <see cref="Code.RefExtensions.ToDurableRef{T}"/> refuses
    /// it with the same explanation as <see cref="IRef.ToDurable"/>.
    /// </summary>
    [Fact]
    public void ToDurableRefOnAnAttributeThrows()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "using System; [Obsolete] class C { }" );

        var attribute = compilation.Types.OfName( "C" ).Single().Attributes.Single();

        Assert.Throws<NotSupportedException>( () => attribute.ToDurableRef() );
    }

    /// <summary>
    /// A durable reference is never an <see cref="IFullRef"/>, and it still has to answer
    /// <see cref="Engine.CodeModel.References.RefExtensions.GetPrimarySyntaxTree(IRef, CompilationContext)"/> with the same tree the equivalent
    /// full reference gives (issue #1748).
    /// </summary>
    /// <remarks>
    /// A durable reference of a batch compilation holds a full reference, but is not one itself. The distinction is
    /// what keeps <see cref="IDurableRef{T}"/> meaningful as the type of a field: a full reference cannot be assigned
    /// to one in any scope.
    /// </remarks>
    [Fact]
    public void GetPrimarySyntaxTreeOfDurableRefInSource()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class C { }" );

        var fullRef = compilation.Types.OfName( "C" ).Single().ToRef();
        var durableRef = fullRef.ToDurable();

        Assert.False( durableRef is IFullRef, "A durable reference is expected never to be a full reference." );

        var expected = fullRef.GetPrimarySyntaxTree( compilation.CompilationContext );
        Assert.NotNull( expected );

        Assert.Same( expected, durableRef.GetPrimarySyntaxTree( compilation.CompilationContext ) );
    }

    /// <summary>
    /// A durable reference to a declaration of a referenced assembly has no syntax tree in the current compilation, so
    /// <see cref="Engine.CodeModel.References.RefExtensions.GetPrimarySyntaxTree(IRef, CompilationContext)"/> returns <c>null</c> rather than
    /// throwing (issue #1748).
    /// </summary>
    [Fact]
    public void GetPrimarySyntaxTreeOfDurableRefInMetadata()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var durableRef = compilation.Factory.GetTypeByReflectionType( typeof(string) ).ToRef().ToDurable();

        Assert.Null( durableRef.GetPrimarySyntaxTree( compilation.CompilationContext ) );
    }

    /// <summary>
    /// A generic type whose fields give the shapes in which a type parameter appears in the type of a declaration.
    /// </summary>
    private const string _typeParameterCode = """
                                              #nullable enable

                                              using System.Collections.Generic;

                                              class C<T>
                                                  where T : class
                                              {
                                                  public T? NullableParameter = null;
                                                  public T NonNullableParameter = null!;
                                                  public List<T?> ListOfNullableParameter = null!;
                                                  public List<T> ListOfNonNullableParameter = null!;
                                              }
                                              """;

    /// <summary>
    /// Verifies that a durable reference to a type that is, or contains, a type parameter round-trips exactly,
    /// including the nullable annotation.
    /// </summary>
    /// <remarks>
    /// <c>T?</c> has to come back as <c>T?</c>, and <c>List&lt;T?&gt;</c> as <c>List&lt;T?&gt;</c>. A type parameter
    /// appears both as the type itself and inside the type arguments of another type, and the second case cannot be
    /// avoided by treating type parameters specially at the top level. See issue #1797.
    /// </remarks>
    [Theory]
    [InlineData( "NullableParameter", true )]
    [InlineData( "NonNullableParameter", false )]
    [InlineData( "ListOfNullableParameter", false )]
    [InlineData( "ListOfNonNullableParameter", false )]
    public void DurableRefToATypeParameterRoundTripsExactly( string fieldName, bool expectedIsNullable )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _typeParameterCode );

        var type = compilation.Types.OfName( "C" ).Single().Fields.OfName( fieldName ).Single().Type;
        Assert.Equal( expectedIsNullable, type.IsNullable );

        // Asserted on the identity and the nullability rather than on a display string, because a display string does
        // not render the nullable annotation and a type parameter does not render identically when it is reached from
        // a field and when it is resolved from an identifier.
        Assert.Equal( expectedIsNullable, type.ToRef().ToDurable().GetTarget( compilation ).IsNullable );
        Assert.True( type.Equals( type.ToRef().ToDurable().GetTarget( compilation ) ) );

        Assert.Equal( expectedIsNullable, type.ToDurableRef().GetTarget( compilation ).IsNullable );
        Assert.True( type.Equals( type.ToDurableRef().GetTarget( compilation ) ) );

        // The annotation of a type argument is the case the outer assertions do not reach: List<T?> is itself a
        // non-nullable List, and only its argument is annotated.
        if ( type is INamedType { TypeArguments.Count: 1 } )
        {
            var expectedArgumentIsNullable = fieldName == "ListOfNullableParameter";

            Assert.Equal( expectedArgumentIsNullable, ((INamedType) type).TypeArguments[0].IsNullable );

            Assert.Equal(
                expectedArgumentIsNullable,
                ((INamedType) type.ToRef().ToDurable().GetTarget( compilation )).TypeArguments[0].IsNullable );

            Assert.Equal(
                expectedArgumentIsNullable,
                ((INamedType) type.ToDurableRef().GetTarget( compilation )).TypeArguments[0].IsNullable );
        }
    }

    /// <summary>
    /// A type whose fields give a nullable and a non-nullable form of the same named type.
    /// </summary>
    private const string _nullableTypesCode = """
                                              #nullable enable

                                              interface IService { }

                                              class Container
                                              {
                                                  public IService? NullableField = null;
                                                  public IService NonNullableField = null!;
                                              }
                                              """;

    /// <summary>
    /// Returns the type of the named field of <c>Container</c> in a compilation of <see cref="_nullableTypesCode"/>.
    /// </summary>
    private static IType GetFieldType( CompilationModel compilation, string fieldName )
        => compilation.Types.OfName( "Container" ).Single().Fields.OfName( fieldName ).Single().Type;

    /// <summary>
    /// Verifies that a durable reference to a nullable named type keeps the nullable annotation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A named type is identified by its documentation identifier, which names the type and nothing else, so the
    /// annotation is not part of what is written. An array or a pointer type is identified by its
    /// <see cref="SerializableTypeId"/> instead, which does carry it. The nullability of a named type is therefore lost
    /// by a conversion that the caller has no reason to think is lossy.
    /// </para>
    /// <para>
    /// It is observable: the dependency injection strategies of <c>Metalama.Extensions</c> hold the type of the
    /// parameter they introduce, and an <c>IService?</c> parameter became an <c>IService</c> one. See issue #1797.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( "NullableField", true )]
    [InlineData( "NonNullableField", false )]
    public void DurableRefToNamedTypeKeepsTheNullability( string fieldName, bool expectedIsNullable )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _nullableTypesCode );

        var type = GetFieldType( compilation, fieldName );
        Assert.Equal( expectedIsNullable, type.IsNullable );

        var throughToDurable = type.ToRef().ToDurable().GetTarget( compilation );
        Assert.Equal( expectedIsNullable, throughToDurable.IsNullable );

        var throughToDurableRef = type.ToDurableRef().GetTarget( compilation );
        Assert.Equal( expectedIsNullable, throughToDurableRef.IsNullable );
    }

    /// <summary>
    /// Verifies that converting an <see cref="INamedType"/> to a durable reference with <c>ToDurable</c> and resolving
    /// it again yields an equivalent type, for every shape of type except a constructed generic one, which
    /// <see cref="DurableRefToConstructedGenericTypeKeepsTheTypeArguments"/> covers.
    /// </summary>
    [Theory]
    [InlineData( "Plain" )]
    [InlineData( "Generic" )]
    [InlineData( "Nested" )]
    [InlineData( "External" )]
    public void DurableRefToNamedTypeResolvesToAnEquivalentType( string kind )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( RefTestFixtures.GenericTypesCode );

        var type = RefTestFixtures.GetTestType( compilation, kind );

        var resolved = type.ToRef().ToDurable().GetTarget( compilation );

        Assert.Equal( type.ToDisplayString(), resolved.ToDisplayString() );
    }

    /// <summary>
    /// Records that a durable reference preserves the type arguments of a constructed generic type, and that a
    /// <see cref="SerializableTypeId"/> does too.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A declaration identifier names a declaration and therefore names the generic definition, so backing
    /// <c>ToDurable</c> with one returned <c>Generic&lt;T&gt;</c> where <c>Generic&lt;int&gt;</c> was converted,
    /// silently and with no diagnostic. That was a trap for any caller converting a type coming from user code,
    /// because the result was a usable type rather than an error and the widening only showed in what the caller
    /// subsequently matched. It broke the constructor parameter pull, which compares the type of the parameter it
    /// introduced against the type it is asked to introduce (issue #1797). A constructed generic type is therefore
    /// identified by its <see cref="SerializableTypeId"/>.
    /// </para>
    /// <para>
    /// <c>Query.CreateBaseTypeResolver</c> was such a caller: <c>SelectTypesDerivedFrom( INamedType )</c> accepts a
    /// constructed generic type, and going through a declaration identifier would have made the query match the types
    /// derived from every construction of the generic type. It builds a <see cref="SerializableTypeId"/> explicitly,
    /// which is no longer needed to avoid the widening, but is still needed because that conversion does not throw for
    /// a type an aspect introduced.
    /// </para>
    /// <para>
    /// A reference that holds the compilation preserves the type arguments trivially, so it is the identifier-based
    /// derived classes that hold this property. That is the reason the derived classes exist.
    /// </para>
    /// </remarks>
    [Fact]
    public void DurableRefToConstructedGenericTypeKeepsTheTypeArguments()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( RefTestFixtures.GenericTypesCode );

        var type = RefTestFixtures.GetTestType( compilation, "Constructed" );
        Assert.Equal( "Generic<int>", type.ToDisplayString() );

        var throughToDurable = type.ToRef().ToDurable().GetTarget( compilation );
        Assert.Equal( "Generic<int>", throughToDurable.ToDisplayString() );

        var throughTypeId = DurableRefFactory.FromTypeId<INamedType>( type.GetSerializableTypeId() ).GetTarget( compilation );
        Assert.Equal( "Generic<int>", throughTypeId.ToDisplayString() );
    }

    /// <summary>
    /// Verifies that a durable reference backed by a <see cref="SerializableDeclarationId"/>, which is what
    /// <see cref="IRef{T}.ToDurable"/> returns for an introduced declaration, resolves to a type introduced into an
    /// introduced namespace.
    /// </summary>
    /// <remarks>
    /// The two kinds of durable identifier are resolved by different code, so both are covered: a type identifier by
    /// <c>SerializableTypeIdResolverForIType</c> and a declaration identifier by <c>DocumentationIdHelper</c>. Both
    /// start their lookup in the merged namespace tree. See issue #1825.
    /// </remarks>
    [Fact]
    public void DurableDeclarationIdRefToTypeIntroducedIntoIntroducedNamespaceResolves()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class Outer;" ).CreateMutableClone();

        var introducedNamespace = RefTestFixtures.IntroduceNamespace( compilation, compilation.GlobalNamespace, "Introduced" );
        var introducedType = RefTestFixtures.IntroduceType( compilation, introducedNamespace, "Companion" );

        IDurableRef<INamedType> durableRef = introducedType.ToRef().ToDurable();

        Assert.Same( introducedType, durableRef.GetTarget( compilation ) );
    }
}
