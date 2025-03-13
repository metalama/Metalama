// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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