// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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