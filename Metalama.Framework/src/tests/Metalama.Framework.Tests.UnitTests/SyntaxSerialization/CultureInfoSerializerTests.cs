// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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