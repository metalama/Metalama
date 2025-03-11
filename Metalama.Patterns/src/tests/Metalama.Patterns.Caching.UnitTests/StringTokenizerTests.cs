// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using Xunit;

namespace Metalama.Patterns.Caching.Tests
{
    public sealed class StringTokenizerTests
    {
        [Fact]
        public void TestGetNext()
        {
            var tokenizer = new StringTokenizer( "1:2:3" );
            Assert.Equal( "1", tokenizer.GetNext( ':' ).ToString() );
            Assert.Equal( "2", tokenizer.GetNext( ':' ).ToString() );
            Assert.Equal( "3", tokenizer.GetNext( ':' ).ToString() );
            Assert.Empty( tokenizer.GetNext( ':' ).ToString() );
            Assert.Empty( tokenizer.GetRemainder().ToString() );
        }

        [Fact]
        public void TestGetRemainder()
        {
            var tokenizer = new StringTokenizer( "1:2:3" );
            Assert.Equal( "1", tokenizer.GetNext( ':' ).ToString() );
            Assert.Equal( "2:3", tokenizer.GetRemainder().ToString() );
            Assert.Empty( tokenizer.GetNext( ':' ).ToString() );
            Assert.Empty( tokenizer.GetRemainder().ToString() );
        }
    }
}