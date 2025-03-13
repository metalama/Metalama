// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class DisplayStringFormatterTests : UnitTestClass
{
    [Theory]
    [InlineData( "int" )]
    [InlineData( "int?" )]
    [InlineData( "string?" )]
    [InlineData( "decimal?" )]
    [InlineData( "(int, string)" )]
    [InlineData( "void" )]
    [InlineData( "Action<int>" )]
    public void Type( string type )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( $"using System; abstract class C {{ public abstract {type} M(); }}" );
        var typeInterface = compilation.Types.Single().Methods.Single().ReturnType;

        Assert.Equal( type, typeInterface.ToDisplayString() );
    }
}