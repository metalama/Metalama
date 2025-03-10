// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class TimeSpanSerializerTests : SerializerTestsBase
    {
        [Fact]
        public void TestTimeSpan()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var ts = TimeSpan.FromMinutes( 38 );
            const long ticks = 38 * TimeSpan.TicksPerMinute;
            Assert.Equal( "new global::System.TimeSpan(" + ticks + "L)", testContext.Serialize( ts ).ToString() );
        }

        [Fact]
        public void TestZero()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            Assert.Equal( "new global::System.TimeSpan(0L)", testContext.Serialize( TimeSpan.Zero ).ToString() );
        }
    }
}