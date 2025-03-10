// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class NullableSerializerTests : SerializerTestsBase
    {
        [Fact]
        public void TestPrimitiveNullables()
        {
            this.AssertSerialization( (float?) 5F, "5F" );
            this.AssertSerialization( (float?) null, "null" );
        }

        [Fact]
        public void TestListOfNullables()
        {
            var list = new List<float?> { 5, null };

            this.AssertSerialization(
                list,
                """
                new global::System.Collections.Generic.List<global::System.Single?>
                {
                    5F,
                    null
                }
                """ );
        }

        private void AssertSerialization<T>( T? o, string expected )
        {
            using var testContext = this.CreateSerializationTestContext( "" );
            var creationExpression = testContext.Serialize( o ).NormalizeWhitespace().ToString();
            Assert.Equal( expected, creationExpression );
        }
    }
}