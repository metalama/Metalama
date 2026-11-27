// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using System.Collections.Concurrent;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Implementation;

/// <summary>
/// Tests that <see cref="InvalidationTombstoneSet"/> keeps the records that a conditional store needs and does not grow
/// beyond the keys invalidated in the last one and a half lifetimes.
/// </summary>
/// <remarks>
/// The set purges the records older than its lifetime when a key is recorded, at most once per half lifetime. The times
/// are passed explicitly, so the tests need no clock.
/// </remarks>
public sealed class InvalidationTombstoneSetTests
{
    private static readonly TimeSpan _lifetime = TimeSpan.FromMinutes( 10 );
    private static readonly DateTimeOffset _origin = new( 2026, 1, 1, 0, 0, 0, TimeSpan.Zero );

    [Fact]
    public void Record_AfterHalfLifetime_PurgesRecordsOlderThanLifetime()
    {
        var set = new InvalidationTombstoneSet( _lifetime );

        set.Record( "old", _origin );
        set.Record( "middle", _origin + TimeSpan.FromMinutes( 6 ) );

        Assert.Equal( 2, set.Count );

        // The purge that runs here removes the records older than 12 - 10 = 2 minutes.
        set.Record( "new", _origin + TimeSpan.FromMinutes( 12 ) );

        Assert.Equal( 2, set.Count );
        Assert.False( set.IsInvalidatedSince( "old", null, _origin ), "The record older than the lifetime was not purged." );
        Assert.True( set.IsInvalidatedSince( "middle", null, _origin + TimeSpan.FromMinutes( 6 ) ), "A record within the lifetime was purged." );
        Assert.True( set.IsInvalidatedSince( "new", null, _origin + TimeSpan.FromMinutes( 12 ) ) );
    }

    [Fact]
    public void Record_WithinHalfLifetimeOfLastPurge_DoesNotPurge()
    {
        var set = new InvalidationTombstoneSet( _lifetime );

        // The first record purges, and the next purge is due five minutes later.
        set.Record( "first", _origin );
        set.Record( "second", _origin + TimeSpan.FromMinutes( 5 ) );

        // The record of "first" is not older than the lifetime yet at the purge of minute 5, and the record at minute 9 does
        // not purge, so the record of "first" is still there at minute 9.
        set.Record( "third", _origin + TimeSpan.FromMinutes( 9 ) );

        Assert.Equal( 3, set.Count );
        Assert.True( set.IsInvalidatedSince( "first", null, _origin ) );
    }

    [Fact]
    public void Record_SustainedInvalidationsOfDistinctKeys_CountStaysBounded()
    {
        var set = new InvalidationTombstoneSet( _lifetime );
        var maxCount = 0;

        // One distinct key per second for ten lifetimes.
        for ( var second = 0; second < 6000; second++ )
        {
            set.Record( "key-" + second, _origin + TimeSpan.FromSeconds( second ) );
            maxCount = Math.Max( maxCount, set.Count );
        }

        // The set holds the keys of the last lifetime, plus the expired keys of at most half a lifetime that the next purge
        // removes.
        var keysPerLifetime = (int) _lifetime.TotalSeconds;

        Assert.InRange( set.Count, keysPerLifetime, (keysPerLifetime * 3 / 2) + 1 );
        Assert.InRange( maxCount, keysPerLifetime, (keysPerLifetime * 3 / 2) + 1 );
    }

    [Fact]
    public void Record_KeyRecordedAgainBeforePurge_KeepsNewRecord()
    {
        var set = new InvalidationTombstoneSet( _lifetime );

        set.Record( "key", _origin );
        set.Record( "other", _origin + TimeSpan.FromMinutes( 6 ) );

        // The key is recorded again by the call that purges. The new time is stored before the purge, so the purge, which
        // removes the records older than minute 2, keeps it.
        set.Record( "key", _origin + TimeSpan.FromMinutes( 12 ) );

        Assert.True( set.IsInvalidatedSince( "key", null, _origin + TimeSpan.FromMinutes( 12 ) ), "The new record of the key was purged." );
    }

    [Fact]
    public void Record_OlderTime_DoesNotMoveRecordBack()
    {
        var set = new InvalidationTombstoneSet( _lifetime );

        set.Record( "key", _origin + TimeSpan.FromSeconds( 5 ) );
        set.Record( "key", _origin + TimeSpan.FromSeconds( 3 ) );

        Assert.True( set.IsInvalidatedSince( "key", null, _origin + TimeSpan.FromSeconds( 4 ) ), "A delayed record moved the time of the key back." );
    }

    [Fact]
    public void IsInvalidatedSince_ChecksKeyAndDependencies()
    {
        var set = new InvalidationTombstoneSet( _lifetime );

        set.Record( "dependency", _origin + TimeSpan.FromSeconds( 1 ) );

        Assert.True( set.IsInvalidatedSince( "key", ["other", "dependency"], _origin + TimeSpan.FromSeconds( 1 ) ) );
        Assert.False( set.IsInvalidatedSince( "key", ["other", "dependency"], _origin + TimeSpan.FromSeconds( 2 ) ) );
        Assert.False( set.IsInvalidatedSince( "key", ["other"], _origin ) );
    }

    /// <summary>
    /// Records keys from several threads while the time advances quickly, so that purges run concurrently with records.
    /// A key must be reported as invalidated immediately after it has been recorded, and the set must stay bounded.
    /// </summary>
    [Fact]
    public async Task Record_ConcurrentWithPurges_KeepsEveryRecentRecord()
    {
        var lifetime = TimeSpan.FromMilliseconds( 100 );
        var set = new InvalidationTombstoneSet( lifetime );
        var ticks = _origin.UtcTicks;
        var failures = new ConcurrentQueue<string>();
        const int keyCount = 50;

        var workers = Enumerable.Range( 0, 8 )
            .Select(
                worker => Task.Run(
                    () =>
                    {
                        for ( var i = 0; i < 20_000; i++ )
                        {
                            // Each record advances the time by one millisecond, so a purge is due every 50 records.
                            var time = new DateTimeOffset( Interlocked.Add( ref ticks, TimeSpan.TicksPerMillisecond ), TimeSpan.Zero );
                            var key = "key-" + (((worker * 7) + i) % keyCount);

                            set.Record( key, time );

                            // Another thread can legitimately purge the record once the time has advanced by more than
                            // the lifetime since the record.
                            if ( !set.IsInvalidatedSince( key, null, time ) && Volatile.Read( ref ticks ) - time.UtcTicks < lifetime.Ticks )
                            {
                                failures.Enqueue( $"The record of {key} at {time:O} was lost." );
                            }
                        }
                    },
                    TestContext.Current.CancellationToken ) )
            .ToArray();

        await Task.WhenAll( workers );

        Assert.Empty( failures );
        Assert.InRange( set.Count, 1, keyCount );
    }
}
