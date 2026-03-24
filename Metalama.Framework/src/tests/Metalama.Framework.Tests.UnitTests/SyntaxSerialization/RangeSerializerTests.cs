// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NET5_0_OR_GREATER

using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class RangeSerializerTests : SerializerTestsBase
    {
        [Fact]
        public void TestRange()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var range = new Range( new Index( 1 ), new Index( 3 ) );

            Assert.Equal(
                "new global::System.Range(new global::System.Index(1, false), new global::System.Index(3, false))",
                testContext.Serialize( range ).ToString() );
        }

        [Fact]
        public void TestRangeWithFromEnd()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var range = new Range( new Index( 0 ), new Index( 1, true ) );

            Assert.Equal(
                "new global::System.Range(new global::System.Index(0, false), new global::System.Index(1, true))",
                testContext.Serialize( range ).ToString() );
        }
    }
}

#endif
