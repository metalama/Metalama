// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Xunit;

// ReSharper disable IdentifierTypo

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class ObjectSerializersTests : SerializerTestsBase
    {
        [Fact]
        public void TestInt()
        {
            this.AssertSerialization( 42, "42" );
        }

        [Fact]
        public void TestNullable()
        {
            int? i = 42;
            this.AssertSerialization( i, "42" );
        }

        [Fact]
        public void TestListInt()
        {
            this.AssertSerialization(
                new List<int> { 4, 6, 8 },
                """
                new global::System.Collections.Generic.List<global::System.Int32>
                {
                    4,
                    6,
                    8
                }
                """ );
        }

        [Fact]
        public void TestString()
        {
            this.AssertSerialization( "hello", "\"hello\"" );
        }

        [Fact]
        public void TestInfiniteRecursion()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Throws<DiagnosticException>(
                () =>
                {
                    var o = new List<object>();
                    o.Add( o );
                    testContext.Serialize( o );
                } );
        }

        [Fact]
        public void TestUnsupportedAnonymousType()
        {
            using var testContext = this.CreateSerializationTestContext( "" );
            Assert.Throws<DiagnosticException>( () => testContext.Serialize( new { A = "F" } ) );
        }

        [Fact]
        public void TestNull()
        {
            this.AssertSerialization( (object?) null, "null" );
        }

        private void AssertSerialization<T>( T? o, string expected )
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var creationExpression = testContext.Serialize( o ).NormalizeWhitespace().ToString();
            Assert.Equal( expected, creationExpression );
        }

        [Fact]
        public void TestEnumsBasic()
        {
            this.AssertSerialization( World.Venus, "global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.World.Venus" );
            this.AssertSerialization( Mars.Moon.Phobos, "global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.Mars.Moon.Phobos" );
        }

        [Fact]
        public void TestNegativeEnum()
        {
            this.AssertSerialization( (LongEnum) (-1), "(global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.LongEnum)(-1L)" );
        }

        [Fact]
        public void TestEnumsFlags()
        {
            this.AssertSerialization(
                WorldFeatures.Icy | WorldFeatures.Volcanic,
                "(global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.WorldFeatures)(9UL)" );

            this.AssertSerialization(
                HumanFeatures.Tall | HumanFeatures.Wise,
                "(global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.HumanFeatures)(9UL)" );

            this.AssertSerialization( WorldFeatures.Icy, "global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.WorldFeatures.Icy" );
        }

        [Fact]
        public void TestEnumsNestedInGenerics()
        {
            this.AssertSerialization(
                Box<int>.Color.Blue | Box<int>.Color.Red,
                "(global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.Box<global::System.Int32>.Color)(12L)" );

            this.AssertSerialization(
                Box<int>.Color.Blue,
                "global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.Box<global::System.Int32>.Color.Blue" );
        }

        [Fact]
        public void TestEnumsTwiceNestedInGenerics()
        {
            this.AssertSerialization(
                Box<int>.InnerBox.Shiny.Yes,
                "global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.Box<global::System.Int32>.InnerBox.Shiny.Yes" );
        }

        [Fact]
        public void TestArray()
        {
            this.AssertSerialization(
                new[] { 1, 2 },
                """
                new global::System.Int32[]
                {
                    1,
                    2
                }
                """ );
        }

        [Fact]
        public void TestEnumsGenericsInGenericMethod()
        {
            this.GenericMethod<float>();
        }

        private void GenericMethod<TK>()
        {
            this.AssertSerialization(
                Box<TK>.Color.Blue,
                "global::Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets.Box<global::System.Single>.Color.Blue" );
        }
    }
}