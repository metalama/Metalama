// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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