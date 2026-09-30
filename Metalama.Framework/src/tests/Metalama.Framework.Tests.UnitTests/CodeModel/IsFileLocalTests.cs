// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class IsFileLocalTests : UnitTestClass
{
    [Theory]
    [InlineData( "file class T { }" )]
    [InlineData( "file struct T { }" )]
    [InlineData( "file interface T { }" )]
    [InlineData( "file enum T { }" )]
    [InlineData( "file record T;" )]
    [InlineData( "file record struct T;" )]
    [InlineData( "file delegate void T();" )]
    public void FileLocalType_IsFileLocal( string code )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( code );
        var type = compilation.Types.OfName( "T" ).Single();

        Assert.True( type.IsFileLocal );
    }

    [Theory]
    [InlineData( "internal class T { }" )]
    [InlineData( "class T { }" )]
    [InlineData( "public class T { }" )]
    public void NonFileLocalType_IsNotFileLocal( string code )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( code );
        var type = compilation.Types.OfName( "T" ).Single();

        Assert.False( type.IsFileLocal );
    }

    [Fact]
    public void TypeNestedInFileLocalType_IsNotFileLocal()
    {
        using var testContext = this.CreateTestContext();

        const string code = """
                            file class Outer
                            {
                                public class Inner { }
                            }
                            """;

        var compilation = testContext.CreateCompilationModel( code );
        var outerType = compilation.Types.OfName( "Outer" ).Single();
        var innerType = outerType.Types.OfName( "Inner" ).Single();

        Assert.True( outerType.IsFileLocal );
        Assert.False( innerType.IsFileLocal );
    }

    [Fact]
    public void FileLocalType_AccessibilityIsInternal()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "file class T { }" );
        var type = compilation.Types.OfName( "T" ).Single();

        Assert.Equal( Accessibility.Internal, type.Accessibility );
    }

    [Fact]
    public void IntroducedType_IsNotFileLocal()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "file class Outer { }" ).CreateMutableClone();

        var typeBuilder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "C", TypeKind.Class );
        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        Assert.False( typeBuilder.IsFileLocal );
        Assert.False( compilation.Types.OfName( "C" ).Single().IsFileLocal );
    }
}
