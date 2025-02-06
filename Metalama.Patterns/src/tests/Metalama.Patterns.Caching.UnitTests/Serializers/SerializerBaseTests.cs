// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Serializers;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Serializers
{
    public abstract class SerializerBaseTests
    {
        private readonly ICachingSerializer _serializer;

        protected SerializerBaseTests( ICachingSerializer serializer )
        {
            this._serializer = serializer;
        }

        private object? RoundTrip( object? cacheItem )
        {
            var memoryStream = new MemoryStream();
            var writer = new BinaryWriter( memoryStream );
            this._serializer.Serialize( cacheItem, writer );
            memoryStream.Seek( 0, SeekOrigin.Begin );
            var reader = new BinaryReader( memoryStream );
            var newCacheItem = this._serializer.Deserialize( reader );

            return newCacheItem;
        }

        [Fact]
        public void TestDictionary()
        {
            var dictionary = new Dictionary<string, int>() { ["1"] = 1, ["2"] = 2 };
            var roundTrip = this.RoundTrip( dictionary );
            Assert.Equivalent( dictionary, roundTrip );
        }

        [Fact]
        public void TestNullValue()
        {
            var roundTrip = this.RoundTrip( null );

            Assert.Null( roundTrip );
        }

        [Fact]
        public void TestObject()
        {
            var o = new MyObject();
            var roundTripItem = (MyObject?) this.RoundTrip( o );
            Assert.Equal( o.Value, roundTripItem!.Value );
        }
    }
}