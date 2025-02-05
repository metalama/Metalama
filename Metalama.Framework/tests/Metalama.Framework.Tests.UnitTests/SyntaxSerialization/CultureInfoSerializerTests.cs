// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Globalization;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class CultureInfoSerializerTests : SerializerTestsBase
    {
        [Fact]
        public void TestCzech()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var ci = new CultureInfo( "cs-CZ", true );
            Assert.Equal( @"new global::System.Globalization.CultureInfo(""cs-CZ"", true)", testContext.Serialize( ci ).ToString() );
        }

        [Fact]
        public void TestSlovakFalse()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var ci = new CultureInfo( "sk-SK", false );
            Assert.Equal( @"new global::System.Globalization.CultureInfo(""sk-SK"", false)", testContext.Serialize( ci ).ToString() );
        }
    }
}