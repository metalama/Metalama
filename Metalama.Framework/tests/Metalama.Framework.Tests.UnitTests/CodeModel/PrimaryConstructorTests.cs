// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class PrimaryConstructorTests : UnitTestClass
{
    [Fact]
    public void ImplicitConstructor_Class()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class A {}
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeClass = compilation.Types.OfName( "A" ).Single();

        Assert.Null( typeClass.PrimaryConstructor );
        Assert.All( typeClass.Constructors, c => Assert.False( c.IsPrimary ) );
    }
    
    [Fact]
    public void ImplicitConstructor_Struct()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
struct B {}
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeStruct = compilation.Types.OfName( "B" ).Single();

        Assert.Null( typeStruct.PrimaryConstructor );
        Assert.All( typeStruct.Constructors, c => Assert.False( c.IsPrimary ) );
    }
    
    [Fact]
    public void ImplicitConstructor_RecordClass()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
record class C {}
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeRecordClass = compilation.Types.OfName( "C" ).Single();

        Assert.Null( typeRecordClass.PrimaryConstructor );
        Assert.All( typeRecordClass.Constructors, c => Assert.False( c.IsPrimary ) );
    }
    
    [Fact]
    public void ImplicitConstructor_RecordStruct()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
record struct D {}
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeRecordStruct = compilation.Types.OfName( "D" ).Single();

        Assert.Null( typeRecordStruct.PrimaryConstructor );
        Assert.All( typeRecordStruct.Constructors, c => Assert.False( c.IsPrimary ) );
    }
    
    [Fact]
    public void ImplicitConstructor_Enum()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
enum E {}
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeEnum = compilation.Types.OfName( "E" ).Single();

        Assert.Null( typeEnum.PrimaryConstructor );
        Assert.All( typeEnum.Constructors, c => Assert.False( c.IsPrimary ) );
    }
    
    [Fact]
    public void ImplicitConstructor_Delegate()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
delegate void F();
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeDelegate = compilation.Types.OfName( "F" ).Single();

        Assert.Null( typeDelegate.PrimaryConstructor );
        Assert.All( typeDelegate.Constructors, c => Assert.False( c.IsPrimary ) );
    }

    [Fact]
    public void ExplicitConstructor_Class()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class A { public A() {} public A(int x) {} }
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeClass = compilation.Types.OfName( "A" ).Single();

        Assert.Null( typeClass.PrimaryConstructor );
        Assert.All( typeClass.Constructors, c => Assert.False( c.IsPrimary ) );
    }

    [Fact]
    public void ExplicitConstructor_Struct()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
struct B { public B() {} public B(int x) {} }
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeStruct = compilation.Types.OfName( "B" ).Single();

        Assert.Null( typeStruct.PrimaryConstructor );
        Assert.All( typeStruct.Constructors, c => Assert.False( c.IsPrimary ) );
    }

    [Fact]
    public void ExplicitConstructor_RecordClass()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
record class C { public C() {} public C(int x) {} }
record struct D { public D() {} public D(int x) {} }
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeRecordClass = compilation.Types.OfName( "C" ).Single();

        Assert.Null( typeRecordClass.PrimaryConstructor );
        Assert.All( typeRecordClass.Constructors, c => Assert.False( c.IsPrimary ) );
    }

    [Fact]
    public void ExplicitConstructor_RecordStruct()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
record struct D { public D() {} public D(int x) {} }
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeRecordStruct = compilation.Types.OfName( "D" ).Single();

        Assert.Null( typeRecordStruct.PrimaryConstructor );
        Assert.All( typeRecordStruct.Constructors, c => Assert.False( c.IsPrimary ) );
    }

#if ROSLYN_4_8_0_OR_GREATER
    [Fact]
    public void ParameterlessPrimaryConstructor_Class()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class A() {}
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeClass = compilation.Types.OfName( "A" ).Single();

        Assert.NotNull( typeClass.PrimaryConstructor );
        Assert.Single( typeClass.Constructors );
        Assert.Equal( typeClass.Constructors.Single(), typeClass.PrimaryConstructor );
    }

    [Fact]
    public void ParameterlessPrimaryConstructor_Struct()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
struct B() {}
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeStruct = compilation.Types.OfName( "B" ).Single();

        Assert.NotNull( typeStruct.PrimaryConstructor );
        Assert.Single( typeStruct.Constructors );
        Assert.Equal( typeStruct.Constructors.Single(), typeStruct.PrimaryConstructor );
    }
#endif

    [Fact]
    public void ParameterlessPrimaryConstructor_RecordClass()
    {
        using var testContext = this.CreateTestContext();

        const string code =
            @"
record class C() {}
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeRecordClass = compilation.Types.OfName( "C" ).Single();

        Assert.NotNull( typeRecordClass.PrimaryConstructor );
        Assert.Equal( 2, typeRecordClass.Constructors.Count );
        Assert.Single( typeRecordClass.Constructors, c => c.Parameters is [] );
        Assert.Single( typeRecordClass.Constructors, c => c.Parameters is [{ }] );
        Assert.Equal( typeRecordClass.Constructors.Single( c => c.Parameters is [] ), typeRecordClass.PrimaryConstructor );
    }

    [Fact]
    public void ParameterlessPrimaryConstructor_RecordStruct()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
record struct D() {}
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeRecordStruct = compilation.Types.OfName( "D" ).Single();

        Assert.NotNull( typeRecordStruct.PrimaryConstructor );
        Assert.Single( typeRecordStruct.Constructors );
        Assert.Equal( typeRecordStruct.Constructors.Single(), typeRecordStruct.PrimaryConstructor );
    }

#if ROSLYN_4_8_0_OR_GREATER
    [Fact]
    public void PrimaryConstructor_Class()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class A(int x) {}
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeClass = compilation.Types.OfName( "A" ).Single();

        Assert.NotNull( typeClass.PrimaryConstructor );
        Assert.Single( typeClass.Constructors );
        Assert.Equal( typeClass.Constructors.Single(), typeClass.PrimaryConstructor );
    }

    [Fact]
    public void PrimaryConstructor_Struct()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
struct B(int x) {}
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeStruct = compilation.Types.OfName( "B" ).Single();

        Assert.NotNull( typeStruct.PrimaryConstructor );
        Assert.Equal( 2, typeStruct.Constructors.Count );
        Assert.Single( typeStruct.Constructors, c => c.Parameters is [{ Type.SpecialType: SpecialType.Int32 }] );
        Assert.Single( typeStruct.Constructors, c => c.Parameters is [] );
        Assert.Equal( typeStruct.Constructors.Single( c => c.Parameters is [{ Type.SpecialType: SpecialType.Int32 }] ), typeStruct.PrimaryConstructor );
    }
#endif

    [Fact]
    public void PrimaryConstructor_RecordClass()
    {
        using var testContext = this.CreateTestContext();

        const string code =
#if NETFRAMEWORK
            @"namespace System.Runtime.CompilerServices { internal static class IsExternalInit {}}" +
#endif
            @"
record class C(int x) {}
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeRecordClass = compilation.Types.OfName( "C" ).Single();

        Assert.NotNull( typeRecordClass.PrimaryConstructor );
        Assert.Equal( 2, typeRecordClass.Constructors.Count );
        Assert.Single( typeRecordClass.Constructors, c => c.Parameters is [{ Type.SpecialType: SpecialType.Int32 }] );
        Assert.Single( typeRecordClass.Constructors, c => c.Parameters is [{ Type.SpecialType: not SpecialType.Int32 }] );

        Assert.Equal(
            typeRecordClass.Constructors.Single( c => c.Parameters is [{ Type.SpecialType: SpecialType.Int32 }] ),
            typeRecordClass.PrimaryConstructor );
    }

    [Fact]
    public void PrimaryConstructor_RecordStruct()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
record struct D(int x) {}
";

        var compilation = testContext.CreateCompilationModel( code );

        var typeRecordStruct = compilation.Types.OfName( "D" ).Single();

        Assert.NotNull( typeRecordStruct.PrimaryConstructor );
        Assert.Equal( 2, typeRecordStruct.Constructors.Count );
        Assert.Single( typeRecordStruct.Constructors, c => c.Parameters is [{ Type.SpecialType: SpecialType.Int32 }] );
        Assert.Single( typeRecordStruct.Constructors, c => c.Parameters is [] );
        Assert.Equal( typeRecordStruct.Constructors.Single( c => c.Parameters.Count == 1 ), typeRecordStruct.PrimaryConstructor );
    }

    [Fact]
    public void InitializerKind()
    {
        using var testContext = this.CreateTestContext();

        var kinds = new[]
        {
#if ROSLYN_4_8_0
            "class",
            "struct",
#endif
            "record",
            "record class",
            "record struct"
        };

        foreach ( var kind in kinds )
        {
            var code = $$"""
                {{kind}} NoInitializer();

                interface IFoo { }

                {{kind}} Interface() : IFoo;
                """;

            if ( !kind.ContainsOrdinal( "struct" ) )
            {
                code += $$"""
                    {{kind}} Base;

                    {{kind}} DerivedSimple() : Base;

                    {{kind}} DerivedInitializer() : Base();
                    """;
            }

            var compilation = testContext.CreateCompilationModel( code );

            var noInitializer = compilation.Types.OfName( "NoInitializer" ).Single();
            Assert.Equal( ConstructorInitializerKind.None, noInitializer.PrimaryConstructor.InitializerKind );

            var @interface = compilation.Types.OfName( "Interface" ).Single();
            Assert.Equal( ConstructorInitializerKind.None, @interface.PrimaryConstructor.InitializerKind );

            if ( !kind.ContainsOrdinal( "struct" ) )
            {
                var derivedSimple = compilation.Types.OfName( "DerivedSimple" ).Single();
                Assert.Equal( ConstructorInitializerKind.None, derivedSimple.PrimaryConstructor.InitializerKind );

                var derivedInitializer = compilation.Types.OfName( "DerivedInitializer" ).Single();
                Assert.Equal( ConstructorInitializerKind.Base, derivedInitializer.PrimaryConstructor.InitializerKind );
            }
        }
    }
}