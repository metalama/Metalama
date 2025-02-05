// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class StringSerializerTests : SerializerTestsBase
    {
        [Fact]
        public void TestString()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "\"Hel\\0lo\\\"\"", testContext.Serialize( "Hel\0lo\"" ).NormalizeWhitespace().ToString() );

            Assert.Equal(
                "\"Hello,\\n world!\"",
                testContext.Serialize( "Hello,\n world!" )
                    .NormalizeWhitespace()
                    .ToString()
                    .ReplaceOrdinal( "\\r", "" ) );

            Assert.Equal( "\"Hello, world!\"", testContext.Serialize( $@"Hello, {"world"}!" ).NormalizeWhitespace().ToString() );
        }
    }
}