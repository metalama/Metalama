// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.TestHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.TestHelpersTests;

/// <summary>
/// Tests of the removal operations of <see cref="FakeMemoryCache"/>.
/// </summary>
/// <remarks>
/// <see cref="FakeMemoryCache.Compact"/> follows <see cref="MemoryCache.Compact"/>, and
/// <see cref="Metalama.Patterns.Caching.Backends.MemoryCachingBackend"/> relies on that contract, so the tests below
/// pin the number of entries that are removed, the order in which they are chosen and the reason that is reported to
/// the post-eviction callbacks.
/// </remarks>
public sealed class FakeMemoryCacheTests
{
    private static readonly DateTimeOffset _origin = new( 2026, 1, 1, 0, 0, 0, TimeSpan.Zero );

    [Fact]
    public void Compact_RemovesTheRequestedFractionOfTheEntries()
    {
        var timeProvider = new FakeTimeProvider( _origin );
        using var cache = new FakeMemoryCache( timeProvider );

        var keys = SetEntries( cache, 4 );

        cache.Compact( 0.5 );

        Assert.Equal( 2, GetPresentKeys( cache, keys ).Count );
    }

    [Fact]
    public void Compact_WithZeroPercentage_RemovesNothing()
    {
        var timeProvider = new FakeTimeProvider( _origin );
        using var cache = new FakeMemoryCache( timeProvider );

        var keys = SetEntries( cache, 4 );

        cache.Compact( 0 );

        Assert.Equal( keys, GetPresentKeys( cache, keys ) );
    }

    [Fact]
    public void Compact_RemovesTheLowestPriorityEntriesFirst()
    {
        var timeProvider = new FakeTimeProvider( _origin );
        using var cache = new FakeMemoryCache( timeProvider );

        cache.Set( "low", 0, new MemoryCacheEntryOptions { Priority = CacheItemPriority.Low } );
        cache.Set( "normal", 0, new MemoryCacheEntryOptions { Priority = CacheItemPriority.Normal } );
        cache.Set( "high", 0, new MemoryCacheEntryOptions { Priority = CacheItemPriority.High } );
        cache.Set( "never", 0, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove } );

        cache.Compact( 0.5 );

        Assert.Equal( new[] { "high", "never" }, GetPresentKeys( cache, new[] { "low", "normal", "high", "never" } ) );
    }

    [Fact]
    public void Compact_WithFullPercentage_KeepsTheEntriesThatAreNeverRemoved()
    {
        var timeProvider = new FakeTimeProvider( _origin );
        using var cache = new FakeMemoryCache( timeProvider );

        cache.Set( "normal", 0 );
        cache.Set( "never", 0, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove } );

        cache.Compact( 1 );

        Assert.Equal( new[] { "never" }, GetPresentKeys( cache, new[] { "normal", "never" } ) );
    }

    [Fact]
    public void Compact_RemovesTheLeastRecentlyAccessedEntryFirst()
    {
        var timeProvider = new FakeTimeProvider( _origin );
        using var cache = new FakeMemoryCache( timeProvider );

        cache.Set( "older", 0 );
        timeProvider.Advance( TimeSpan.FromMinutes( 1 ) );
        cache.Set( "newer", 0 );

        cache.Compact( 0.5 );

        Assert.Equal( new[] { "newer" }, GetPresentKeys( cache, new[] { "older", "newer" } ) );
    }

    [Fact]
    public void Compact_ReportsTheCapacityReasonToThePostEvictionCallbacks()
    {
        var timeProvider = new FakeTimeProvider( _origin );
        using var cache = new FakeMemoryCache( timeProvider );

        EvictionReason? reason = null;

        cache.Set(
            "key",
            0,
            new MemoryCacheEntryOptions().RegisterPostEvictionCallback( ( _, _, evictionReason, _ ) => reason = evictionReason ) );

        cache.Compact( 1 );

        Assert.Equal( EvictionReason.Capacity, reason );
    }

    [Theory]
    [InlineData( -0.5 )]
    [InlineData( 1.5 )]
    public void Compact_WithAPercentageOutOfRange_ThrowsArgumentOutOfRangeException( double percentage )
    {
        var timeProvider = new FakeTimeProvider( _origin );
        using var cache = new FakeMemoryCache( timeProvider );

        Assert.Throws<ArgumentOutOfRangeException>( () => cache.Compact( percentage ) );
    }

    [Fact]
    public void Clear_RemovesEveryEntry_IncludingTheEntriesThatAreNeverRemoved()
    {
        var timeProvider = new FakeTimeProvider( _origin );
        using var cache = new FakeMemoryCache( timeProvider );

        cache.Set( "normal", 0 );
        cache.Set( "never", 0, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove } );

        cache.Clear();

        Assert.Empty( GetPresentKeys( cache, new[] { "normal", "never" } ) );
    }

    /// <summary>
    /// Adds the given number of entries, which have no expiration and the default priority.
    /// </summary>
    private static string[] SetEntries( IMemoryCache cache, int count )
    {
        var keys = Enumerable.Range( 0, count ).Select( i => "key" + i ).ToArray();

        foreach ( var key in keys )
        {
            cache.Set( key, 0 );
        }

        return keys;
    }

    /// <summary>
    /// Returns the keys, in the given order, of the entries that are still present in the cache.
    /// </summary>
    private static List<string> GetPresentKeys( IMemoryCache cache, IEnumerable<string> keys )
        => keys.Where( key => cache.TryGetValue( key, out _ ) ).ToList();
}
