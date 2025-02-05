// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class ArrayTests : SerializerTestsBase
    {
        [Fact]
        public void TestBasicArray()
        {
            this.AssertSerialization(
                new int[4],
                """
                new global::System.Int32[]
                {
                    0,
                    0,
                    0,
                    0
                }
                """ );

            this.AssertSerialization(
                new[] { 10, 20, 30 },
                """
                new global::System.Int32[]
                {
                    10,
                    20,
                    30
                }
                """ );

            this.AssertSerialization(
                new[] { Mars.Moon.Deimos },
                """
                new global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.Mars.Moon[]
                {
                    global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.Mars.Moon.Deimos
                }
                """ );
        }

        [Fact]
        public void TestArrayOfLists()
        {
            this.AssertSerialization(
                new[] { new List<int> { 2 } },
                """
                new global::System.Collections.Generic.List<global::System.Int32>[]
                {
                    new global::System.Collections.Generic.List<global::System.Int32>
                    {
                        2
                    }
                }
                """ );
        }

        [Fact]
        public void TestArrayOfArrays()
        {
            this.AssertSerialization(
                new[] { new[] { 1, 2 } },
                """
                new global::System.Int32[][]
                {
                    new global::System.Int32[]
                    {
                        1,
                        2
                    }
                }
                """ );
        }

        [Fact]
        public void TestMultiArray()
        {
            Assert.Throws<DiagnosticException>( () => this.AssertSerialization( new[,] { { 2 } }, "new System.Int32[,]{{2}}" ) );
        }

        private void AssertSerialization( object o, string expected )
        {
            using var testContext = this.CreateSerializationTestContext( "" );
            var creationExpression = testContext.SerializationService.Serialize( o, testContext.SerializationContext ).NormalizeWhitespace().ToString();
            Assert.Equal( expected, creationExpression );
        }
    }
}