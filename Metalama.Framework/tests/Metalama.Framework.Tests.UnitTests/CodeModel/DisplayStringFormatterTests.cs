// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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