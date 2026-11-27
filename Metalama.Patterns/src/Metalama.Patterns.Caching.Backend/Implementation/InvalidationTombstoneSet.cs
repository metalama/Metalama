// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Concurrent;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Records, for a limited time, the time at which each key was last invalidated. A backend uses it to detect that the
/// key or a dependency of an item was invalidated while the value of the item was computed.
/// </summary>
/// <remarks>
/// Each time is a number of UTC ticks. The records that are older than the lifetime are purged when a key is recorded,
/// at most once per half lifetime, so that the set does not grow with the number of invalidated keys.
/// </remarks>
internal sealed class InvalidationTombstoneSet
{
    private readonly ConcurrentDictionary<string, long> _invalidationTimes = new( StringComparer.Ordinal );
    private readonly long _lifetimeTicks;
    private long _lastPurgeTicks;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidationTombstoneSet"/> class.
    /// </summary>
    /// <param name="lifetime">The duration for which an invalidation is recorded.</param>
    public InvalidationTombstoneSet( TimeSpan lifetime )
    {
        if ( lifetime <= TimeSpan.Zero )
        {
            throw new ArgumentOutOfRangeException( nameof(lifetime), "The lifetime must be strictly positive." );
        }

        this.Lifetime = lifetime;
        this._lifetimeTicks = lifetime.Ticks;
    }

    /// <summary>
    /// Gets the duration for which an invalidation is recorded.
    /// </summary>
    public TimeSpan Lifetime { get; }

    /// <summary>
    /// Gets the number of records, including the expired records that have not been purged yet.
    /// </summary>
    internal int Count => this._invalidationTimes.Count;

    /// <summary>
    /// Records that a key has been invalidated. Must be called before the items of the key are removed, so that a
    /// concurrent store that does not see the record has registered its item before the removal looks for it.
    /// </summary>
    /// <param name="key">The invalidated key, which is the key of an item or a dependency key.</param>
    /// <param name="time">The time of the invalidation.</param>
    public void Record( string key, DateTimeOffset time )
    {
        var ticks = time.UtcTicks;

        this._invalidationTimes.AddOrUpdate( key, ticks, ( _, previous ) => Math.Max( previous, ticks ) );

        this.PurgeIfDue( ticks );
    }

    /// <summary>
    /// Determines whether a key or one of the dependencies of its item has been invalidated at or after a given time.
    /// </summary>
    /// <param name="key">The key of the item.</param>
    /// <param name="dependencies">The dependencies of the item.</param>
    /// <param name="since">The time from which an invalidation is reported.</param>
    /// <returns><see langword="true"/> when an invalidation has been recorded at or after <paramref name="since"/>.</returns>
    public bool IsInvalidatedSince( string key, IEnumerable<string>? dependencies, DateTimeOffset since )
    {
        if ( this._invalidationTimes.IsEmpty )
        {
            return false;
        }

        var sinceTicks = since.UtcTicks;

        if ( this.IsKeyInvalidatedSince( key, sinceTicks ) )
        {
            return true;
        }

        if ( dependencies != null )
        {
            foreach ( var dependency in dependencies )
            {
                if ( this.IsKeyInvalidatedSince( dependency, sinceTicks ) )
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsKeyInvalidatedSince( string key, long sinceTicks )
        => this._invalidationTimes.TryGetValue( key, out var ticks ) && ticks >= sinceTicks;

    private void PurgeIfDue( long nowTicks )
    {
        var lastPurgeTicks = Volatile.Read( ref this._lastPurgeTicks );

        if ( nowTicks - lastPurgeTicks < this._lifetimeTicks / 2
             || Interlocked.CompareExchange( ref this._lastPurgeTicks, nowTicks, lastPurgeTicks ) != lastPurgeTicks )
        {
            return;
        }

        var expiredTicks = nowTicks - this._lifetimeTicks;

        foreach ( var pair in this._invalidationTimes )
        {
            if ( pair.Value < expiredTicks )
            {
                // The record is removed only if it has not been updated since it was read.
                ((ICollection<KeyValuePair<string, long>>) this._invalidationTimes).Remove( pair );
            }
        }
    }
}
