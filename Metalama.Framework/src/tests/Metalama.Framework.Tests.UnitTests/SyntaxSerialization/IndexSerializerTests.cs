// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if !NET48

using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class IndexSerializerTests : SerializerTestsBase
    {
        [Fact]
        public void TestIndexFromStart()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var index = new Index( 5 );
            Assert.Equal( "new global::System.Index(5, false)", testContext.Serialize( index ).ToString() );
        }

        [Fact]
        public void TestIndexFromEnd()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var index = new Index( 3, true );
            Assert.Equal( "new global::System.Index(3, true)", testContext.Serialize( index ).ToString() );
        }

        [Fact]
        public void TestIndexZero()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var index = new Index( 0 );
            Assert.Equal( "new global::System.Index(0, false)", testContext.Serialize( index ).ToString() );
        }
    }
}

#endif
