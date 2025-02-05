// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Globalization;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class DateTimeOffsetSerializerTests : SerializerTestsBase
    {
        [Fact]
        public void TestDateTimeOffset()
        {
            this.AssertDateTimeSerialization( new DateTimeOffset( 2000, 1, 1, 14, 42, 22, TimeSpan.Zero ) );
            this.AssertDateTimeSerialization( new DateTimeOffset( 2000, 1, 1, 14, 42, 22, TimeSpan.FromHours( 1 ) ) );
            this.AssertDateTimeSerialization( DateTimeOffset.MinValue );
            this.AssertDateTimeSerialization( DateTimeOffset.MaxValue );
        }

        private void AssertDateTimeSerialization( DateTimeOffset dateTime )
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var dt = dateTime;
            Assert.Equal( dateTime, DateTimeOffset.Parse( dateTime.ToString( "o" ), CultureInfo.InvariantCulture ) );
            Assert.Equal( "global::System.DateTimeOffset.Parse(\"" + dateTime.ToString( "o" ) + "\")", testContext.Serialize( dt ).ToString() );
        }
    }
}