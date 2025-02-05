// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class ListSerializerTests : SerializerTestsBase
    {
        [Fact]
        public void TestEmptyList()
        {
            this.AssertSerialization(
                new List<float>(),
                """
                new global::System.Collections.Generic.List<global::System.Single>
                {
                }
                """ );
        }

        [Fact]
        public void TestBasicList()
        {
            this.AssertSerialization(
                new List<float> { 1, 2, 3 },
                """
                new global::System.Collections.Generic.List<global::System.Single>
                {
                    1F,
                    2F,
                    3F
                }
                """ );
        }

        [Fact]
        public void TestListInList()
        {
            this.AssertSerialization(
                new List<List<int>> { new() { 1 } },
                """
                new global::System.Collections.Generic.List<global::System.Collections.Generic.List<global::System.Int32>>
                {
                    new global::System.Collections.Generic.List<global::System.Int32>
                    {
                        1
                    }
                }
                """ );
        }

        [Fact]
        public void TestInfiniteRecursion()
        {
            var l = new List<IList>();
            l.Add( l );

            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Throws<DiagnosticException>( () => testContext.Serialize( l ) );
        }

        private void AssertSerialization<T>( List<T> o, string expected )
        {
            using var testContext = this.CreateSerializationTestContext( "" );
            var creationExpression = testContext.Serialize( o ).NormalizeWhitespace().ToString();
            Assert.Equal( expected, creationExpression );
        }
    }
}