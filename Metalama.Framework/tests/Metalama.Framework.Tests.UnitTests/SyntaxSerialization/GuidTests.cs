// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    public sealed class GuidTests : SerializerTestsBase
    {
        [Fact]
        public void TestGuid()
        {
            using var testContext = this.CreateSerializationTestContext( "" );

            var guid = new Guid( new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 } );
            Assert.Equal( guid, new Guid( 67305985, 1541, 2055, 9, 10, 11, 12, 13, 14, 15, 16 ) );
            Assert.Equal( "new global::System.Guid(67305985, 1541, 2055, 9, 10, 11, 12, 13, 14, 15, 16)", testContext.Serialize( guid ).ToString() );
        }
    }
}