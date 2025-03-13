// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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