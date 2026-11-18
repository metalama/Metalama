// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
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

    private static MetadataReference ToImageReference( Compilation compilation )
    {
        using var stream = new System.IO.MemoryStream();
        var result = compilation.Emit( stream );
        Assert.True( result.Success );

        return MetadataReference.CreateFromImage( stream.ToArray() );
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

    [Fact]
    public void ConstructorWithParameterOfTypeArgumentFromReferencedTopLevelStatementsProgram()
    {
        using var testContext = this.CreateTestContext();

        var factoryAssembly = testContext.CreateCSharpCompilation(
            """
            namespace Microsoft.AspNetCore.Mvc.Testing
            {
                public class WebApplicationFactory<TEntryPoint> where TEntryPoint : class { }
            }
            """,
            assemblyName: "TheFactory" );

        var webAssembly = testContext.CreateCSharpCompilation(
            """
            System.Console.WriteLine( "Hello" );

            public partial class Program { }
            """,
            assemblyName: "TheWeb",
            outputKind: OutputKind.ConsoleApplication );

        var compilation = testContext.CreateCompilationModel(
            """
            using Microsoft.AspNetCore.Mvc.Testing;

            namespace MyNamespace
            {
                public class MyTests
                {
                    public MyTests( WebApplicationFactory<Program> factory ) { }
                }
            }
            """,
            additionalReferences: [ToImageReference( factoryAssembly ), ToImageReference( webAssembly )] );

        AssertRoundTrips( GetType( compilation, "MyTests" ).Constructors.Single() );
    }
}
