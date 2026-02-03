// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.Serializers;
using System.Collections.Immutable;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Serializers;

public sealed class SerializerEdgeCaseTests
{

    #region CacheItemSerializer Tests

    [Fact]
    public void CacheItemSerializer_Deserialize_InvalidMarker_ThrowsInvalidCacheItemException()
    {
        var serializer = new CacheItemSerializer( new JsonCachingSerializer() );

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        // Write an invalid marker (neither 0 nor 1)
        writer.Write( (byte) 99 );
        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        Assert.Throws<InvalidCacheItemException>( () => serializer.Deserialize( reader, ImmutableArray<string>.Empty ) );
    }

    [Fact]
    public void CacheItemSerializer_RoundTrip_DefaultCacheItem_PreservesValue()
    {
        var serializer = new CacheItemSerializer( new JsonCachingSerializer() );
        var originalValue = new MyObject { Value = 42 };
        var originalItem = new CacheItem( originalValue );

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        serializer.Serialize( originalItem, writer );
        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        var deserialized = serializer.Deserialize( reader, ImmutableArray<string>.Empty );

        Assert.NotNull( deserialized );
        Assert.IsType<MyObject>( deserialized.Value );
        Assert.Equal( originalValue.Value, ((MyObject) deserialized.Value!).Value );
    }

    [Fact]
    public void CacheItemSerializer_RoundTrip_MaterializedCacheItem_PreservesConfiguration()
    {
        var serializer = new CacheItemSerializer( new JsonCachingSerializer() );
        var originalValue = new MyObject { Value = 123 };

        var config = new CacheItemConfiguration
        {
            SlidingExpiration = TimeSpan.FromMinutes( 30 ), AbsoluteExpiration = TimeSpan.FromHours( 2 ), Priority = CacheItemPriority.High
        };

        var originalItem = new MaterializedCacheItem( new CacheItem( originalValue, ImmutableArray<string>.Empty, config ) );

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        serializer.Serialize( originalItem, writer );
        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        var deserialized = serializer.Deserialize( reader, ImmutableArray<string>.Empty );

        Assert.NotNull( deserialized );
        Assert.IsType<MaterializedCacheItem>( deserialized );

        var materializedDeserialized = (MaterializedCacheItem) deserialized;
        Assert.Equal( config.SlidingExpiration, materializedDeserialized.SlidingExpiration );
        Assert.Equal( config.Priority, materializedDeserialized.Priority );
    }

    #endregion

    #region JsonCachingSerializer Tests

    [Fact]
    public void JsonSerializer_Serialize_NullValue_WritesNullMarker()
    {
        var serializer = new JsonCachingSerializer();

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        serializer.Serialize( null, writer );
        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        var result = serializer.Deserialize( reader );

        Assert.Null( result );
    }

    [Fact]
    public void JsonSerializer_Deserialize_NullMarker_ReturnsNull()
    {
        var serializer = new JsonCachingSerializer();

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        // Write null marker directly
        writer.Write( (byte) 0 );
        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        var result = serializer.Deserialize( reader );

        Assert.Null( result );
    }

    [Fact]
    public void JsonSerializer_Deserialize_InvalidMarker_ThrowsInvalidCacheItemException()
    {
        var serializer = new JsonCachingSerializer();

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        // Write an invalid marker (neither 0 nor 1)
        writer.Write( (byte) 42 );
        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        Assert.Throws<InvalidCacheItemException>( () => serializer.Deserialize( reader ) );
    }

    [Fact]
    public void JsonSerializer_Deserialize_MissingType_ThrowsException()
    {
        var serializer = new JsonCachingSerializer();

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        // Write object marker
        writer.Write( (byte) 1 );

        // Write a non-existent type name
        writer.Write( "NonExistent.Type.That.Does.Not.Exist, NonExistentAssembly" );

        // Write some JSON payload
        writer.Write( "{}" );
        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        // Throws either TypeLoadException or FileNotFoundException depending on the runtime
        Assert.ThrowsAny<Exception>( () => serializer.Deserialize( reader ) );
    }

    [Fact]
    public void JsonSerializer_RoundTrip_SimpleObject_PreservesValue()
    {
        var serializer = new JsonCachingSerializer();
        var originalObject = new MyObject { Value = 42 };

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        serializer.Serialize( originalObject, writer );
        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        var result = serializer.Deserialize( reader );

        Assert.NotNull( result );
        Assert.IsType<MyObject>( result );
        Assert.Equal( originalObject.Value, ((MyObject) result).Value );
    }

    [Fact]
    public void JsonSerializer_RoundTrip_ComplexObject_PreservesValue()
    {
        var serializer = new JsonCachingSerializer();
        var originalObject = new ComplexObject { Id = 1, Name = "Test", Items = new List<string> { "a", "b", "c" }, Nested = new MyObject { Value = 99 } };

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        serializer.Serialize( originalObject, writer );
        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        var result = serializer.Deserialize( reader );

        Assert.NotNull( result );
        Assert.IsType<ComplexObject>( result );

        var complex = (ComplexObject) result;
        Assert.Equal( originalObject.Id, complex.Id );
        Assert.Equal( originalObject.Name, complex.Name );
        Assert.Equal( originalObject.Items.Count, complex.Items.Count );
        Assert.NotNull( complex.Nested );
        Assert.Equal( originalObject.Nested.Value, complex.Nested.Value );
    }

    [Fact]
    public void JsonSerializer_RoundTrip_PrimitiveTypes()
    {
        var serializer = new JsonCachingSerializer();

        // Test various primitive types
        var testCases = new object[]
        {
            42, 3.14, "hello world", true, DateTime.UtcNow.Date // Use Date to avoid precision issues
        };

        foreach ( var original in testCases )
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter( memoryStream );

            serializer.Serialize( original, writer );
            writer.Flush();

            memoryStream.Seek( 0, SeekOrigin.Begin );
            using var reader = new BinaryReader( memoryStream );

            var result = serializer.Deserialize( reader );

            Assert.NotNull( result );
            Assert.Equal( original.GetType(), result.GetType() );
            Assert.Equal( original, result );
        }
    }

    #endregion

    #region MaterializedCacheItem Edge Cases

    [Fact]
    public void MaterializedCacheItem_RoundTrip_NoExpiration_PreservesNulls()
    {
        var serializer = new CacheItemSerializer( new JsonCachingSerializer() );
        var originalValue = new MyObject { Value = 1 };

        // Create item with no expiration configured
        var config = new CacheItemConfiguration();
        var originalItem = new MaterializedCacheItem( new CacheItem( originalValue, ImmutableArray<string>.Empty, config ) );

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        serializer.Serialize( originalItem, writer );
        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        var deserialized = (MaterializedCacheItem) serializer.Deserialize( reader, ImmutableArray<string>.Empty );

        Assert.Null( deserialized.SlidingExpiration );
        Assert.Null( deserialized.Priority );
    }

    [Fact]
    public void MaterializedCacheItem_RoundTrip_AllPriorities()
    {
        var serializer = new CacheItemSerializer( new JsonCachingSerializer() );

        foreach ( CacheItemPriority priority in Enum.GetValues( typeof(CacheItemPriority) ) )
        {
            var config = new CacheItemConfiguration { Priority = priority };
            var originalItem = new MaterializedCacheItem( new CacheItem( "value", ImmutableArray<string>.Empty, config ) );

            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter( memoryStream );

            serializer.Serialize( originalItem, writer );
            writer.Flush();

            memoryStream.Seek( 0, SeekOrigin.Begin );
            using var reader = new BinaryReader( memoryStream );

            var deserialized = (MaterializedCacheItem) serializer.Deserialize( reader, ImmutableArray<string>.Empty );

            Assert.Equal( priority, deserialized.Priority );
        }
    }

    [Fact]
    public void MaterializedCacheItem_RoundTrip_ZeroSlidingExpiration_DeserializesAsNull()
    {
        var serializer = new CacheItemSerializer( new JsonCachingSerializer() );

        // Zero sliding expiration is serialized as 0 milliseconds, which deserializes as null
        // This is expected behavior - zero sliding expiration is treated as "no sliding expiration"
        var config = new CacheItemConfiguration { SlidingExpiration = TimeSpan.Zero };
        var originalItem = new MaterializedCacheItem( new CacheItem( "value", ImmutableArray<string>.Empty, config ) );

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        serializer.Serialize( originalItem, writer );
        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        var deserialized = (MaterializedCacheItem) serializer.Deserialize( reader, ImmutableArray<string>.Empty );

        // Zero TimeSpan serializes as 0 milliseconds, which reads back as null (no sliding expiration)
        Assert.Null( deserialized.SlidingExpiration );
    }

    [Fact]
    public void MaterializedCacheItem_RoundTrip_VeryLongSlidingExpiration()
    {
        var serializer = new CacheItemSerializer( new JsonCachingSerializer() );

        var longExpiration = TimeSpan.FromDays( 365 );
        var config = new CacheItemConfiguration { SlidingExpiration = longExpiration };
        var originalItem = new MaterializedCacheItem( new CacheItem( "value", ImmutableArray<string>.Empty, config ) );

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        serializer.Serialize( originalItem, writer );
        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        var deserialized = (MaterializedCacheItem) serializer.Deserialize( reader, ImmutableArray<string>.Empty );

        Assert.Equal( longExpiration, deserialized.SlidingExpiration );
    }

    #endregion

    #region Corrupted Data Tests

    [Fact]
    public void CacheItemSerializer_Deserialize_TruncatedData_ThrowsException()
    {
        var serializer = new CacheItemSerializer( new JsonCachingSerializer() );

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        // Write a valid marker but truncate the rest
        writer.Write( (byte) 0 ); // Default cache item marker

        // Don't write the actual data - just the marker

        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        // Should throw EndOfStreamException or similar when trying to read more data
        Assert.Throws<EndOfStreamException>( () => serializer.Deserialize( reader, ImmutableArray<string>.Empty ) );
    }

    [Fact]
    public void JsonSerializer_Deserialize_TruncatedData_ThrowsException()
    {
        var serializer = new JsonCachingSerializer();

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter( memoryStream );

        // Write object marker
        writer.Write( (byte) 1 );

        // Write type name but no JSON payload
        writer.Write( typeof(MyObject).AssemblyQualifiedName! );

        // Don't write JSON payload
        writer.Flush();

        memoryStream.Seek( 0, SeekOrigin.Begin );
        using var reader = new BinaryReader( memoryStream );

        Assert.Throws<EndOfStreamException>( () => serializer.Deserialize( reader ) );
    }

    #endregion

    #region Test Classes

    internal class ComplexObject
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public List<string> Items { get; set; } = new();

        public MyObject? Nested { get; set; }
    }

    #endregion
}