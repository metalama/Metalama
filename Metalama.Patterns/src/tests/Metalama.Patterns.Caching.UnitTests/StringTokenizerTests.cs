// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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