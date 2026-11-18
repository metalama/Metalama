// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Verifies that <see cref="SerializableDeclarationId.Resolve"/> returns the declaration whose
/// <see cref="IDeclaration.ToSerializableId"/> produced the identifier. See issue #2358.
/// </summary>
public sealed class SerializableDeclarationIdRoundTripTests : UnitTestClass
{
    private const string _code = """
                                 namespace MyNamespace
                                 {
                                     public class MyType<T>
                                     {
                                         public MyType( T value ) { }
                                         public MyType( System.Guid id ) { }
                                     }

                                     public class MyPair<T1, T2>
                                     {
                                         public MyPair( T1 a, T2 b, T1 c, T2 d ) { }
                                         public MyPair<T1, T2> WithSameOutputs( T1 a, T2 b ) => this;
                                     }

                                     public class MyModel
                                     {
                                         public int Item { get; set; }
                                     }

                                     public class MyIndexed
                                     {
                                         public int this[int index] => index;
                                     }

                                     public class MyClass
                                     {
                                         public int MyField;
                                         public int MyProperty { get; }
                                         public MyClass( int value ) { this.MyProperty = value; }
                                     }
                                 }
                                 """;

    private static void AssertRoundTrips( IDeclaration declaration )
    {
        var id = declaration.ToSerializableId();
        var resolved = id.Resolve( declaration.Compilation );

        Assert.Same( declaration, resolved );
    }

    private static INamedType GetType( ICompilation compilation, string name ) => compilation.Types.OfName( name ).Single();

    [Fact]
    public void ConstructorOfGenericTypeWithTypeParameterParameter()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var constructor = GetType( compilation, "MyType" ).Constructors.Single( c => c.Parameters[0].Type is ITypeParameter );

        AssertRoundTrips( constructor );
    }

    [Fact]
    public void ConstructorOfGenericTypeWithSeveralTypeParameterParameters()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        AssertRoundTrips( GetType( compilation, "MyPair" ).Constructors.Single() );
    }

    [Fact]
    public void ConstructorOfGenericTypeWithNonGenericParameter()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var constructor = GetType( compilation, "MyType" ).Constructors.Single( c => c.Parameters[0].Type is not ITypeParameter );

        AssertRoundTrips( constructor );
    }

    [Fact]
    public void MethodOfGenericTypeWithTypeParameterParameters()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        AssertRoundTrips( GetType( compilation, "MyPair" ).Methods.OfName( "WithSameOutputs" ).Single() );
    }

    [Fact]
    public void PropertyNamedItem()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        AssertRoundTrips( GetType( compilation, "MyModel" ).Properties.OfName( "Item" ).Single() );
    }

    [Fact]
    public void Indexer()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        AssertRoundTrips( GetType( compilation, "MyIndexed" ).Indexers.Single() );
    }

    [Fact]
    public void PseudoSetterOfGetOnlyProperty()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var property = GetType( compilation, "MyClass" ).Properties.OfName( "MyProperty" ).Single();

        Assert.Equal( Writeability.ConstructorOnly, property.Writeability );

        AssertRoundTrips( property.SetMethod.AssertNotNull() );
    }

    [Fact]
    public void PseudoGetterOfField()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var field = GetType( compilation, "MyClass" ).Fields.OfName( "MyField" ).Single();

        AssertRoundTrips( field.GetMethod.AssertNotNull() );
    }

    /// <summary>
    /// Verifies that a constructor with parameters resolves in a compilation that does not reference Metalama.Framework,
    /// such as a test project that references the web project it tests.
    /// </summary>
    [Fact]
    public void ConstructorInCompilationWithoutMetalamaReference()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel(
            """
            namespace MyNamespace
            {
                public class MyTests
                {
                    public MyTests( System.Collections.Generic.List<MyTests> list ) { }
                }
            }
            """,
            addMetalamaReferences: false );

        AssertRoundTrips( GetType( compilation, "MyTests" ).Constructors.Single() );
    }
}
