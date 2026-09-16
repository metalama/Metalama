// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Formatters;
using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Formatters;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests;

/// <summary>
/// Tests of the cache key of a method whose parameter is a union of C# 15. See finding UT-14c of issue #1946.
/// </summary>
/// <remarks>
/// A union declaration is a struct for which the compiler synthesizes no <c>ToString</c>, so the default formatter
/// resolved <see cref="System.ValueType.ToString"/> and produced the name of the union type for every value. Every
/// case of the union then had the same cache key, and a cached method returned the entry of one case for another.
/// </remarks>
public sealed class UnionCacheKeyTests : BaseCachingTests
{
    public UnionCacheKeyTests( ITestOutputHelper testOutputHelper )
        : base( testOutputHelper ) { }

    private sealed class KeyRecordingCacheKeyBuilder : CacheKeyBuilder
    {
        public KeyRecordingCacheKeyBuilder( IFormatterRepository formatterRepository, CacheKeyBuilderOptions options )
            : base( formatterRepository, options ) { }

        public string? LastMethodKey { get; private set; }

        public override string BuildMethodKey( CachedMethodMetadata metadata, object? instance, IList<object?> arguments )
            => this.LastMethodKey = base.BuildMethodKey( metadata, instance, arguments );
    }

    private const string _profileName = "Caching.Tests.UnionCacheKeyTests";

    [Cache( ProfileName = _profileName )]
    private static string Describe( CachedShape shape ) => shape.Value?.ToString() ?? "none";

    /// <summary>
    /// Verifies that two cases of the same union produce two cache keys, and that the second call is therefore not
    /// served from the entry of the first.
    /// </summary>
    [Fact]
    public void TwoCasesProduceTwoKeys()
    {
        using var context = this.InitializeTest( _profileName, b => b.WithKeyBuilder( ( f, o ) => new KeyRecordingCacheKeyBuilder( f, o ) ) );

        var keyBuilder = (KeyRecordingCacheKeyBuilder) CachingService.Default.KeyBuilder;

        var circleResult = Describe( new CachedCircle( 1 ) );
        var circleKey = keyBuilder.LastMethodKey;

        var squareResult = Describe( new CachedSquare( 1 ) );
        var squareKey = keyBuilder.LastMethodKey;

        this.TestOutputHelper.WriteLine( circleKey );
        this.TestOutputHelper.WriteLine( squareKey );

        Assert.NotEqual( circleKey, squareKey );
        Assert.NotEqual( circleResult, squareResult );
    }

    /// <summary>
    /// Verifies that two cases whose values format to the same text produce two cache keys, because the key holds the
    /// type of the case as well as its value.
    /// </summary>
    [Fact]
    public void TwoCasesOfTheSameNumberProduceTwoKeys()
    {
        using var context = this.InitializeTest( _profileName, b => b.WithKeyBuilder( ( f, o ) => new KeyRecordingCacheKeyBuilder( f, o ) ) );

        var keyBuilder = (KeyRecordingCacheKeyBuilder) CachingService.Default.KeyBuilder;

        Count( 1 );
        var intKey = keyBuilder.LastMethodKey;

        Count( 1L );
        var longKey = keyBuilder.LastMethodKey;

        this.TestOutputHelper.WriteLine( intKey );
        this.TestOutputHelper.WriteLine( longKey );

        Assert.NotEqual( intKey, longKey );
    }

    [Cache( ProfileName = _profileName )]
    private static string Count( CachedNumber number ) => number.Value?.ToString() ?? "none";
}

public record CachedCircle( double Radius );

public record CachedSquare( double Side );

public union CachedShape( CachedCircle, CachedSquare );

public union CachedNumber( int, long );
