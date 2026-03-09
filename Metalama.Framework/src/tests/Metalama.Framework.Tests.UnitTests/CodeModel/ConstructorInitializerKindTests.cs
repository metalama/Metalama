// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class ConstructorInitializerKindTests : UnitTestClass
{
    [Fact]
    public void Class_NoExplicitInitializer_ReturnsBase()
    {
        // A class constructor with no explicit initializer implicitly calls base().
        using var testContext = this.CreateTestContext();

        const string code = """
            class SomeClass
            {
                public SomeClass() { }
            }
            """;

        var compilation = testContext.CreateCompilationModel( code );
        var type = compilation.Types.OfName( "SomeClass" ).Single();
        var constructor = type.Constructors.Single();

        Assert.Equal( ConstructorInitializerKind.Base, constructor.InitializerKind );
    }

    [Fact]
    public void Class_ExplicitThis_ReturnsThis()
    {
        // A class constructor with explicit : this() should return This.
        using var testContext = this.CreateTestContext();

        const string code = """
            class SomeClass
            {
                private readonly string _name;

                public SomeClass() : this( "" ) { }

                public SomeClass( string name )
                {
                    this._name = name;
                }
            }
            """;

        var compilation = testContext.CreateCompilationModel( code );
        var type = compilation.Types.OfName( "SomeClass" ).Single();
        var ctorNoParam = type.Constructors.Single( c => c.Parameters.Count == 0 );
        var ctorWithParam = type.Constructors.Single( c => c.Parameters.Count == 1 );

        Assert.Equal( ConstructorInitializerKind.This, ctorNoParam.InitializerKind );
        Assert.Equal( ConstructorInitializerKind.Base, ctorWithParam.InitializerKind );
    }

    [Fact]
    public void Class_ExplicitBase_ReturnsBase()
    {
        // A class constructor with explicit : base() should return Base.
        using var testContext = this.CreateTestContext();

        const string code = """
            class BaseClass
            {
                public BaseClass( int x ) { }
            }

            class DerivedClass : BaseClass
            {
                public DerivedClass() : base( 42 ) { }
            }
            """;

        var compilation = testContext.CreateCompilationModel( code );
        var type = compilation.Types.OfName( "DerivedClass" ).Single();
        var constructor = type.Constructors.Single();

        Assert.Equal( ConstructorInitializerKind.Base, constructor.InitializerKind );
    }

    [Fact]
    public void Struct_NoExplicitInitializer_ReturnsNone()
    {
        // A struct constructor with no explicit initializer truly has no base call.
        using var testContext = this.CreateTestContext();

        const string code = """
            struct SomeStruct
            {
                public int Value;
                public SomeStruct( int value )
                {
                    this.Value = value;
                }
            }
            """;

        var compilation = testContext.CreateCompilationModel( code );
        var type = compilation.Types.OfName( "SomeStruct" ).Single();
        var constructor = type.Constructors.Single( c => c.Parameters.Count == 1 );

        Assert.Equal( ConstructorInitializerKind.None, constructor.InitializerKind );
    }

    [Fact]
    public void Class_ImplicitConstructor_ReturnsBase()
    {
        // The compiler-generated implicit constructor of a class calls base().
        using var testContext = this.CreateTestContext();

        const string code = """
            class SomeClass
            {
            }
            """;

        var compilation = testContext.CreateCompilationModel( code );
        var type = compilation.Types.OfName( "SomeClass" ).Single();
        var constructor = type.Constructors.Single();

        Assert.Equal( ConstructorInitializerKind.Base, constructor.InitializerKind );
    }

    [Fact]
    public void Class_DerivedNoExplicitInitializer_ReturnsBase()
    {
        // A derived class constructor without explicit initializer implicitly calls base().
        using var testContext = this.CreateTestContext();

        const string code = """
            class BaseClass { }

            class DerivedClass : BaseClass
            {
                public DerivedClass() { }
            }
            """;

        var compilation = testContext.CreateCompilationModel( code );
        var type = compilation.Types.OfName( "DerivedClass" ).Single();
        var constructor = type.Constructors.Single();

        Assert.Equal( ConstructorInitializerKind.Base, constructor.InitializerKind );
    }

    [Fact]
    public void Class_StaticConstructor_ReturnsNone()
    {
        // A static constructor never calls base() or this().
        using var testContext = this.CreateTestContext();

        const string code = """
            class SomeClass
            {
                static SomeClass() { }
            }
            """;

        var compilation = testContext.CreateCompilationModel( code );
        var type = compilation.Types.OfName( "SomeClass" ).Single();
        var staticCtor = type.StaticConstructor;

        Assert.NotNull( staticCtor );
        Assert.Equal( ConstructorInitializerKind.None, staticCtor!.InitializerKind );
    }
}
