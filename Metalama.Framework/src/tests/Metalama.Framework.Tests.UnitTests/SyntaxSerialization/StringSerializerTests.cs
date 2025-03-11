// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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