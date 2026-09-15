// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Serializers;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Serializers;

/// <summary>
/// Tests of the serialization of a union of C# 15 by <see cref="JsonCachingSerializer"/>. See finding UT-14c of issue
/// #1946.
/// </summary>
/// <remarks>
/// The <c>Value</c> property of a union has no setter, so the default conversion of
/// <see cref="System.Text.Json.JsonSerializer"/> writes the value of the current case and reads back the default value
/// of the union, whose <c>Value</c> is <c>null</c>. A cached value of union type was therefore lost whenever the
/// caching back-end serialized it.
/// </remarks>
public sealed class UnionSerializerTests
{
    private static object? RoundTrip( object? cacheItem )
    {
        var serializer = new JsonCachingSerializer();

        var memoryStream = new MemoryStream();
        var writer = new BinaryWriter( memoryStream );
        serializer.Serialize( cacheItem, writer );
        memoryStream.Seek( 0, SeekOrigin.Begin );
        var reader = new BinaryReader( memoryStream );

        return serializer.Deserialize( reader );
    }

    /// <summary>
    /// Verifies that the value of the case that the union carries survives the round trip, for each of the two cases.
    /// </summary>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void TheCaseValueSurvivesTheRoundTrip( bool firstCase )
    {
        var value = firstCase ? new SerializedShape( new SerializedCircle( 2.5 ) ) : new SerializedShape( new SerializedSquare( 3.5 ) );

        var roundTrip = (SerializedShape) RoundTrip( value )!;

        Assert.Equal( value.Value, roundTrip.Value );
    }

    /// <summary>
    /// Verifies that two cases whose values are equal numbers are told apart, because the type of the case is written
    /// as well as its value.
    /// </summary>
    [Fact]
    public void TwoCasesOfTheSameNumberAreToldApart()
    {
        var asInt = (SerializedNumber) RoundTrip( new SerializedNumber( 1 ) )!;
        var asLong = (SerializedNumber) RoundTrip( new SerializedNumber( 1L ) )!;

        Assert.IsType<int>( asInt.Value );
        Assert.IsType<long>( asLong.Value );
    }

    /// <summary>
    /// Verifies that the default value of a union, whose <c>Value</c> property is <c>null</c>, survives the round trip.
    /// </summary>
    [Fact]
    public void TheDefaultValueSurvivesTheRoundTrip()
    {
        var roundTrip = (SerializedShape) RoundTrip( default(SerializedShape) )!;

        Assert.Null( roundTrip.Value );
    }
}

public record SerializedCircle( double Radius );

public record SerializedSquare( double Side );

public union SerializedShape( SerializedCircle, SerializedSquare );

public union SerializedNumber( int, long );
