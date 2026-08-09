// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
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

public sealed class RefTests : UnitTestClass
{
    [Fact]
    public void CompilationRef()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var compilationRef = compilation.ToRef();
        var resolved = compilationRef.GetTarget( compilation );

        Assert.Same( compilation, resolved );
    }

    [Fact]
    public void CompilationSymbolId()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );
        var symbolId = SymbolId.Create( compilation.Symbol );
        var resolvedSymbol = symbolId.Resolve( compilation.RoslynCompilation ).AssertNotNull();
        var resolvedDeclaration = compilation.Factory.GetCompilationElement( resolvedSymbol );

        Assert.Same( compilation, resolvedDeclaration );
    }

    [Fact]
    public void ReferencedAssemblySymbol()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var assemblyRefSymbol = compilation.Factory.GetTypeByReflectionType( typeof(string) ).GetSymbol();
        var assemblyRefRef = SymbolId.Create( assemblyRefSymbol );
        _ = assemblyRefRef.Resolve( compilation.RoslynCompilation );
    }

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
    /// <see cref="Code.RefExtensions.ToDurableRef{T}"/> computes the identifier from the declaration instead of from the
    /// symbol of an intermediate <c>IFullRef</c>, so the two routes go through different code. A parameter, a return
    /// parameter and a type parameter are included because their identifiers are built from the identifier of the
    /// containing declaration rather than by the general case.
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
        var compilation = testContext.CreateCompilationModel( _genericTypesCode );

        var type = GetTestType( compilation, "Constructed" );
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
    /// A durable reference is not bound to a compilation, but it still has to answer
    /// <see cref="Engine.CodeModel.References.RefExtensions.GetPrimarySyntaxTree(IRef, CompilationContext)"/> with the same tree the equivalent
    /// full reference gives (issue #1748).
    /// </summary>
    [Fact]
    public void GetPrimarySyntaxTreeOfDurableRefInSource()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class C { }" );

        var fullRef = compilation.Types.OfName( "C" ).Single().ToRef();
        var durableRef = fullRef.ToDurable();

        Assert.False( durableRef is IFullRef, "The reference is expected not to be bound to a compilation." );

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
    /// A durable reference whose id resolves to nothing in the current compilation, which happens when the referenced
    /// project changed since its manifest was written, must not throw either (issue #1748).
    /// </summary>
    [Fact]
    public void GetPrimarySyntaxTreeOfUnresolvableDurableRef()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var durableRef = new DeclarationIdRef<INamedType>( new SerializableDeclarationId( "T:ThereIsNoSuchType" ) );

        Assert.Null( durableRef.GetPrimarySyntaxTree( compilation.CompilationContext ) );
    }

    /// <summary>
    /// The code that <see cref="OldFormatIdentifiersStillResolve"/> resolves its hardcoded identifiers against.
    /// </summary>
    private const string _backwardCompatibilityCode = """
                                                      namespace Ns
                                                      {
                                                          public class C<T>
                                                          {
                                                              public int Field;

                                                              public int M( string p ) => 0;
                                                          }

                                                          public class Plain { }
                                                      }
                                                      """;

    /// <summary>
    /// Verifies that the identifiers written by an earlier version still resolve.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A durable reference to a type is now written as a <see cref="SerializableTypeId"/>, so the identifier of a named
    /// type changed from the documentation form <c>T:Ns.Plain</c> to the type form <c>Y:global::Ns.Plain!</c>. These
    /// identifiers are written into the transitive manifest, which one version of Metalama writes and another reads,
    /// so the old form has to keep resolving. The literals below are hardcoded on purpose: computing them from the
    /// current code would test nothing, because it would produce the new form.
    /// </para>
    /// <para>
    /// The identifiers of declarations that are not types are unchanged, and are included so that a future change to
    /// the format is measured against all of them rather than against types alone. See issue #1797.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( "T:Ns.Plain", "Plain" )]
    [InlineData( "T:Ns.C`1", "C<T>" )]
    [InlineData( "M:Ns.C`1.M(System.String)", "M" )]
    [InlineData( "F:Ns.C`1.Field", "Field" )]
    [InlineData( "M:Ns.C`1.M(System.String);Parameter;0", "p" )]
    [InlineData( "T:Ns.C`1;TypeParameter;0", "T" )]
    public void OldFormatIdentifiersStillResolve( string id, string expectedName )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _backwardCompatibilityCode );

        var resolved = new SerializableDeclarationId( id ).ResolveToDeclaration( compilation );

        Assert.NotNull( resolved );
        Assert.Equal( expectedName, resolved is INamedType namedType ? namedType.ToDisplayString() : ((INamedDeclaration) resolved!).Name );
    }

    /// <summary>
    /// Verifies that a durable reference built from an identifier of the old form resolves, which is the route the
    /// deserializer takes when it reads a manifest written by an earlier version.
    /// </summary>
    [Fact]
    public void DurableRefFromAnOldFormatTypeIdentifierResolves()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _backwardCompatibilityCode );

        var durableRef = DurableRefFactory.FromDeclarationId<INamedType>( new SerializableDeclarationId( "T:Ns.Plain" ) );

        Assert.Equal( "Plain", durableRef.GetTarget( compilation ).ToDisplayString() );
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

    private const string _genericTypesCode = """
                                             class Plain { }

                                             class Generic<T>
                                             {
                                                 public class Nested { }
                                             }

                                             class Container
                                             {
                                                 public Generic<int> ConstructedField = null!;
                                                 public Generic<string> OtherConstructedField = null!;
                                             }
                                             """;

    private static INamedType GetTestType( CompilationModel compilation, string kind )
        => kind switch
        {
            "Plain" => compilation.Types.OfName( "Plain" ).Single(),
            "Generic" => compilation.Types.OfName( "Generic" ).Single(),
            "Nested" => compilation.Types.OfName( "Generic" ).Single().Types.OfName( "Nested" ).Single(),
            "Constructed" => (INamedType) compilation.Types.OfName( "Container" ).Single().Fields.OfName( "ConstructedField" ).Single().Type,
            "External" => compilation.Factory.GetTypeByReflectionType( typeof(string) ).AssertCast<INamedType>(),
            _ => throw new AssertionFailedException( $"Unknown kind '{kind}'." )
        };

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
        var compilation = testContext.CreateCompilationModel( _genericTypesCode );

        var type = GetTestType( compilation, kind );

        var resolved = type.ToRef().ToDurable().GetTarget( compilation );

        Assert.Equal( type.ToDisplayString(), resolved.ToDisplayString() );
    }

    /// <summary>
    /// Records that <c>ToDurable</c> does not preserve the type arguments of a constructed generic type, and that a
    /// <see cref="SerializableTypeId"/> does.
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
    /// </remarks>
    [Fact]
    public void DurableRefToConstructedGenericTypeKeepsTheTypeArguments()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _genericTypesCode );

        var type = GetTestType( compilation, "Constructed" );
        Assert.Equal( "Generic<int>", type.ToDisplayString() );

        var throughToDurable = type.ToRef().ToDurable().GetTarget( compilation );
        Assert.Equal( "Generic<int>", throughToDurable.ToDisplayString() );

        var throughTypeId = DurableRefFactory.FromTypeId<INamedType>( type.GetSerializableTypeId() ).GetTarget( compilation );
        Assert.Equal( "Generic<int>", throughTypeId.ToDisplayString() );
    }

    /// <summary>
    /// Verifies that the durable form used by <c>Query.CreateBaseTypeResolver</c> round-trips every shape of type that
    /// <c>SelectTypesDerivedFrom( INamedType )</c> accepts.
    /// </summary>
    /// <remarks>
    /// That method converts the type it is given to a durable reference so that the query, which may outlive the
    /// compilation by an entire editing session, does not pin it (issue #1799). The type comes from user code, so the
    /// conversion has to survive every shape the signature accepts, not only the plain named type that the first
    /// version of the change was written against.
    /// </remarks>
    [Theory]
    [InlineData( "Plain" )]
    [InlineData( "Generic" )]
    [InlineData( "Nested" )]
    [InlineData( "Constructed" )]
    [InlineData( "External" )]
    public void DurableTypeIdRefResolvesToAnEquivalentType( string kind )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _genericTypesCode );

        var type = GetTestType( compilation, kind );

        var resolved = DurableRefFactory.FromTypeId<INamedType>( type.GetSerializableTypeId() ).GetTarget( compilation );

        Assert.Equal( type.ToDisplayString(), resolved.ToDisplayString() );
    }

    private const string _constrainedGenericTypesCode = """
                                                        using System.Collections.Generic;

                                                        class StructConstrained<T> where T : struct
                                                        {
                                                            public List<T> Field = null!;
                                                        }

                                                        class UnmanagedConstrained<T> where T : unmanaged { }

                                                        class ClassConstrained<T> where T : class { }

                                                        class NotNullConstrained<T> where T : notnull { }

                                                        class Unconstrained<T> { }

                                                        class ConstrainedContainer
                                                        {
                                                            public StructConstrained<int> ConstructedField = null!;

                                                            public void Method<T>( List<T> parameter ) where T : struct { }
                                                        }
                                                        """;

    /// <summary>
    /// Verifies that a durable reference to a generic type definition resolves back to an equivalent type, whatever
    /// constrains its type parameter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The identifier of a named type carries the nullability of the outermost type as a trailing <c>!</c> and, when a
    /// type parameter appears in it, the declaration that declares the parameter as a generic context. Resolving it
    /// applies the nullability of the outermost type to every name in the identifier, so the type parameters of the
    /// definition are annotated as well, and annotating a parameter constrained to be a value type threw. See issue
    /// #1835, and issue #1837 for why the annotation is applied to them at all.
    /// </para>
    /// <para>
    /// An unconstrained parameter does not reproduce the failure, because its nullability is unknown rather than
    /// false, which the code model answers before it examines whether the type is a value type. The constraints are
    /// therefore enumerated rather than represented by a single case.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( "StructConstrained" )]
    [InlineData( "UnmanagedConstrained" )]
    [InlineData( "ClassConstrained" )]
    [InlineData( "NotNullConstrained" )]
    [InlineData( "Unconstrained" )]
    public void DurableRefToGenericTypeDefinitionWithConstrainedTypeParameterResolves( string typeName )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _constrainedGenericTypesCode );

        var type = compilation.Types.OfName( typeName ).Single();

        Assert.Equal( type.ToDisplayString(), type.ToRef().ToDurable().GetTarget( compilation ).ToDisplayString() );
        Assert.Equal( type.ToDisplayString(), type.ToDurableRef().GetTarget( compilation ).ToDisplayString() );
    }

    /// <summary>
    /// Verifies that a durable reference to a constructed generic type whose definition constrains its type parameter
    /// to be a value type resolves back to an equivalent type.
    /// </summary>
    /// <remarks>
    /// This is the shape the derived type has in issue #1835: the constraint is on the definition, and the reference
    /// that the referencing project resolves is a construction of it.
    /// </remarks>
    [Fact]
    public void DurableRefToConstructedGenericTypeWithValueTypeConstraintResolves()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _constrainedGenericTypesCode );

        var type = compilation.Types.OfName( "ConstrainedContainer" ).Single().Fields.OfName( "ConstructedField" ).Single().Type;
        Assert.Equal( "StructConstrained<int>", type.ToDisplayString() );

        Assert.Equal( type.ToDisplayString(), type.ToDurableRef().GetTarget( compilation ).ToDisplayString() );
    }

    /// <summary>
    /// Verifies that a durable reference to a type that mentions a type parameter constrained to be a value type
    /// resolves back to an equivalent type, whether the parameter is declared by a type or by a method.
    /// </summary>
    /// <remarks>
    /// The parameter appears here as a type argument of another generic type rather than as a parameter of the
    /// declaration being referenced, which is the position that the nullability of the outermost type reaches. The
    /// generic context of the identifier is the declaration that declares the parameter, so both a type and a method
    /// are covered: they are resolved by different branches of <c>GetGenericContext</c>.
    /// </remarks>
    [Theory]
    [InlineData( "StructConstrained", "Field" )]
    [InlineData( "ConstrainedContainer", "Method" )]
    public void DurableRefToTypeMentioningAValueTypeConstrainedTypeParameterResolves( string typeName, string memberName )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _constrainedGenericTypesCode );

        var declaringType = compilation.Types.OfName( typeName ).Single();

        var type = memberName == "Field"
            ? declaringType.Fields.OfName( memberName ).Single().Type
            : declaringType.Methods.OfName( memberName ).Single().Parameters[0].Type;

        Assert.Equal( "List<T>", type.ToDisplayString() );

        Assert.Equal( type.ToDisplayString(), type.ToDurableRef().GetTarget( compilation ).ToDisplayString() );
    }

    /// <summary>
    /// Verifies that a durable reference to a type that an aspect introduced into the global namespace resolves back
    /// to that type.
    /// </summary>
    /// <remarks>
    /// The resolution of an identifier starts in the namespace tree merged over the compilation and its references,
    /// whereas an aspect introduces a type into the tree of <see cref="IAssembly.GlobalNamespace"/>. The global
    /// namespace has a distinct declaration in each tree, so the introduced type is added to the collections of both.
    /// See issue #1825.
    /// </remarks>
    [Fact]
    public void DurableTypeIdRefToTypeIntroducedIntoGlobalNamespaceResolves()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class Outer;" ).CreateMutableClone();

        var introducedType = IntroduceType( compilation, compilation.GlobalNamespace, "Introduced" );

        var durableRef = DurableRefFactory.FromTypeId<INamedType>( introducedType.GetSerializableTypeId() );

        Assert.Same( introducedType, durableRef.GetTarget( compilation ) );
    }

    /// <summary>
    /// Verifies that a durable reference to a type that an aspect introduced into a namespace that the aspect also
    /// introduced resolves back to that type.
    /// </summary>
    /// <remarks>
    /// This is the shape of the metrics sample, whose aspect introduces its type into a namespace of its own. The
    /// introduced namespace is added to the global namespace of both trees, and the introduced type is added to the
    /// single collection of that namespace, which has one declaration. See issue #1825.
    /// </remarks>
    [Fact]
    public void DurableTypeIdRefToTypeIntroducedIntoIntroducedNamespaceResolves()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class Outer;" ).CreateMutableClone();

        var introducedNamespace = IntroduceNamespace( compilation, compilation.GlobalNamespace, "Introduced" );
        var introducedType = IntroduceType( compilation, introducedNamespace, "Companion" );

        var durableRef = DurableRefFactory.FromTypeId<INamedType>( introducedType.GetSerializableTypeId() );

        Assert.Same( introducedType, durableRef.GetTarget( compilation ) );
    }

    /// <summary>
    /// Verifies that a durable reference to a type that an aspect introduced into a namespace which a referenced
    /// assembly declares as well resolves back to that type.
    /// </summary>
    /// <remarks>
    /// The namespace has two constituents, so Roslyn creates a merged namespace, and that namespace has a declaration
    /// in each tree. The type is introduced into the declaration of the tree of <see cref="IAssembly.GlobalNamespace"/>
    /// and must also be added to the declaration of the merged tree. A namespace declared by this compilation alone
    /// would not cover this case, because Roslyn then returns the single constituent and one declaration exists.
    /// <c>System</c> is used because every compilation references an assembly that declares it. See issue #1825.
    /// </remarks>
    [Fact]
    public void DurableTypeIdRefToTypeIntroducedIntoMergedNamespaceResolves()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "namespace System { class Outer; }" ).CreateMutableClone();

        var mergedNamespace = compilation.GlobalNamespace.GetDescendant( "System" ).AssertNotNull();
        var introducedType = IntroduceType( compilation, mergedNamespace, "Companion" );

        var durableRef = DurableRefFactory.FromTypeId<INamedType>( introducedType.GetSerializableTypeId() );

        Assert.Same( introducedType, durableRef.GetTarget( compilation ) );
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

        var introducedNamespace = IntroduceNamespace( compilation, compilation.GlobalNamespace, "Introduced" );
        var introducedType = IntroduceType( compilation, introducedNamespace, "Companion" );

        IDurableRef<INamedType> durableRef = introducedType.ToRef().ToDurable();

        Assert.Same( introducedType, durableRef.GetTarget( compilation ) );
    }

    /// <summary>
    /// Introduces a namespace into a mutable compilation and returns the resulting declaration.
    /// </summary>
    private static INamespace IntroduceNamespace( CompilationModel compilation, INamespace containingNamespace, string name )
    {
        var namespaceBuilder = new NamespaceBuilder( null!, containingNamespace, name );
        compilation.AddTransformation( namespaceBuilder.CreateTransformation() );

        return containingNamespace.Namespaces.OfName( name ).AssertNotNull();
    }

    /// <summary>
    /// Introduces a type into a mutable compilation and returns the resulting declaration.
    /// </summary>
    private static INamedType IntroduceType( CompilationModel compilation, INamespace containingNamespace, string name )
    {
        var typeBuilder = new NamedTypeBuilder( null!, containingNamespace, name, TypeKind.Class );
        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        return containingNamespace.Types.OfName( name ).Single();
    }
}