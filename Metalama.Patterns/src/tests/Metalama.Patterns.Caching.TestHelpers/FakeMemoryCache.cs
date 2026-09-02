// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using CacheItemPriority = Microsoft.Extensions.Caching.Memory.CacheItemPriority;

namespace Metalama.Patterns.Caching.TestHelpers;

/// <summary>
/// An implementation of <see cref="IMemoryCache"/> that reads a <see cref="TimeProvider"/> instead of the wall clock.
/// </summary>
/// <remarks>
/// <para>
/// An entry expires as soon as the <see cref="TimeProvider"/> passes its expiration instant. The class registers a
/// timer with the <see cref="TimeProvider"/> for the earliest expiration instant, so advancing a
/// <c>FakeTimeProvider</c> evicts the entries that fall due and invokes their post-eviction callbacks, without the
/// test having to notify the cache. An entry that is read after its expiration instant is also evicted at that
/// point, so correctness does not depend on the timer.
/// </para>
/// <para>
/// The post-eviction callbacks run on the thread that causes the eviction. The caching backend then queues its own
/// work item through its <see cref="IWorkItemDispatcher"/>, which is where a test observes the completion.
/// </para>
/// </remarks>
public sealed class FakeMemoryCache : IClearableMemoryCache
{
    private readonly TimeProvider _timeProvider;
    private readonly object _sync = new();
    private readonly Dictionary<object, Entry> _entries = new();
    private readonly ITimer _timer;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeMemoryCache"/> class.
    /// </summary>
    /// <param name="timeProvider">The clock that decides when an entry expires.</param>
    public FakeMemoryCache( TimeProvider timeProvider )
    {
        this._timeProvider = timeProvider;

        this._timer = timeProvider.CreateTimer(
            static state => ((FakeMemoryCache) state!).EvictExpiredEntries(),
            this,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan );
    }

    /// <inheritdoc />
    public ICacheEntry CreateEntry( object key )
    {
        this.ThrowIfDisposed();

        return new Entry( this, key );
    }

    /// <inheritdoc />
    public bool TryGetValue( object key, out object? value )
    {
        this.ThrowIfDisposed();

        Entry? expiredEntry = null;

        lock ( this._sync )
        {
            if ( !this._entries.TryGetValue( key, out var entry ) )
            {
                value = null;

                return false;
            }

            var now = this._timeProvider.GetUtcNow();

            if ( entry.IsExpired( now ) )
            {
                this._entries.Remove( key );
                expiredEntry = entry;
                value = null;
            }
            else
            {
                entry.OnAccessed( now );
                value = entry.Value;
            }
        }

        if ( expiredEntry != null )
        {
            expiredEntry.NotifyEvicted( EvictionReason.Expired );
            this.RescheduleTimer();

            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void Remove( object key )
    {
        this.ThrowIfDisposed();

        Entry? removedEntry;

        lock ( this._sync )
        {
            if ( !this._entries.TryGetValue( key, out removedEntry ) )
            {
                return;
            }

            this._entries.Remove( key );
        }

        removedEntry.NotifyEvicted( EvictionReason.Removed );
        this.RescheduleTimer();
    }

    /// <inheritdoc />
    /// <remarks>
    /// As with <see cref="MemoryCache.Clear"/>, the entries whose priority is
    /// <see cref="CacheItemPriority.NeverRemove"/> are removed as well.
    /// </remarks>
    public void Clear()
    {
        this.ThrowIfDisposed();

        List<Entry> removedEntries;

        lock ( this._sync )
        {
            removedEntries = this._entries.Values.ToList();
            this._entries.Clear();
        }

        this.NotifyEvicted( removedEntries, EvictionReason.Removed );
    }

    /// <inheritdoc />
    /// <remarks>
    /// The implementation follows <see cref="MemoryCache.Compact"/>. The number of entries to remove is
    /// <paramref name="percentage"/> of the current number of entries, rounded down. The expired entries are removed
    /// first and count towards that number. The remaining entries are then removed by ascending
    /// <see cref="CacheItemPriority"/> and, within a priority, from the least recently accessed. An entry whose
    /// priority is <see cref="CacheItemPriority.NeverRemove"/> is kept unless it has expired.
    /// </remarks>
    public void Compact( double percentage )
    {
        if ( percentage is < 0 or > 1 )
        {
            throw new ArgumentOutOfRangeException( nameof(percentage), percentage, "The percentage must be between 0 and 1." );
        }

        this.ThrowIfDisposed();

        List<Entry> expiredEntries;
        List<Entry> compactedEntries;

        lock ( this._sync )
        {
            var now = this._timeProvider.GetUtcNow();
            var removalCountTarget = (int) (this._entries.Count * percentage);

            expiredEntries = this._entries.Values.Where( e => e.IsExpired( now ) ).ToList();

            compactedEntries = this._entries.Values
                .Where( e => !e.IsExpired( now ) && e.Priority != CacheItemPriority.NeverRemove )
                .OrderBy( e => e.Priority )
                .ThenBy( e => e.LastAccess )
                .Take( Math.Max( removalCountTarget - expiredEntries.Count, 0 ) )
                .ToList();

            foreach ( var entry in expiredEntries.Concat( compactedEntries ) )
            {
                this._entries.Remove( entry.Key );
            }
        }

        this.NotifyEvicted( expiredEntries, EvictionReason.Expired );
        this.NotifyEvicted( compactedEntries, EvictionReason.Capacity );
    }

    /// <summary>
    /// Invokes the post-eviction callbacks of the given entries and reschedules the timer.
    /// </summary>
    private void NotifyEvicted( List<Entry> entries, EvictionReason reason )
    {
        foreach ( var entry in entries )
        {
            entry.NotifyEvicted( reason );
        }

        this.RescheduleTimer();
    }

    private void Add( Entry entry )
    {
        Entry? replacedEntry;

        lock ( this._sync )
        {
            this._entries.TryGetValue( entry.Key, out replacedEntry );
            this._entries[entry.Key] = entry;
        }

        replacedEntry?.NotifyEvicted( EvictionReason.Replaced );
        this.RescheduleTimer();
    }

    private void EvictExpiredEntries()
    {
        List<Entry> expiredEntries;

        lock ( this._sync )
        {
            var now = this._timeProvider.GetUtcNow();
            expiredEntries = this._entries.Values.Where( e => e.IsExpired( now ) ).ToList();

            foreach ( var entry in expiredEntries )
            {
                this._entries.Remove( entry.Key );
            }
        }

        this.NotifyEvicted( expiredEntries, EvictionReason.Expired );
    }

    /// <summary>
    /// Sets the timer to the earliest expiration instant of the entries, so that advancing the
    /// <see cref="TimeProvider"/> evicts them.
    /// </summary>
    private void RescheduleTimer()
    {
        DateTimeOffset? earliestExpiration;
        DateTimeOffset now;

        lock ( this._sync )
        {
            if ( this._disposed )
            {
                return;
            }

            now = this._timeProvider.GetUtcNow();
            earliestExpiration = null;

            foreach ( var entry in this._entries.Values )
            {
                var expiration = entry.GetExpirationInstant();

                if ( expiration != null && (earliestExpiration == null || expiration < earliestExpiration) )
                {
                    earliestExpiration = expiration;
                }
            }
        }

        if ( earliestExpiration == null )
        {
            this._timer.Change( Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan );
        }
        else
        {
            var delay = earliestExpiration.Value - now;
            this._timer.Change( delay > TimeSpan.Zero ? delay : TimeSpan.Zero, Timeout.InfiniteTimeSpan );
        }
    }

    private void ThrowIfDisposed()
    {
        if ( this._disposed )
        {
            throw new ObjectDisposedException( nameof(FakeMemoryCache) );
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock ( this._sync )
        {
            if ( this._disposed )
            {
                return;
            }

            this._disposed = true;
            this._entries.Clear();
        }

        this._timer.Dispose();
    }

    /// <summary>
    /// An entry of a <see cref="FakeMemoryCache"/>. The entry is added to the cache when it is disposed, which is
    /// what <see cref="CacheExtensions.Set{TItem}(IMemoryCache,object,TItem,MemoryCacheEntryOptions)"/> does.
    /// </summary>
    private sealed class Entry : ICacheEntry
    {
        private readonly FakeMemoryCache _cache;

        private DateTimeOffset _lastAccess;
        private int _evicted;

        public Entry( FakeMemoryCache cache, object key )
        {
            this._cache = cache;
            this.Key = key;
            this._lastAccess = cache._timeProvider.GetUtcNow();
        }

        public object Key { get; }

        public object? Value { get; set; }

        public DateTimeOffset? AbsoluteExpiration { get; set; }

        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

        public TimeSpan? SlidingExpiration { get; set; }

        public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();

        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();

        public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;

        public long? Size { get; set; }

        /// <summary>
        /// Gets the instant at which the entry has last been read, or at which it has been created when it has never
        /// been read.
        /// </summary>
        public DateTimeOffset LastAccess => this._lastAccess;

        /// <summary>
        /// Records that the entry has been read, which restarts the sliding expiration.
        /// </summary>
        public void OnAccessed( DateTimeOffset now ) => this._lastAccess = now;

        /// <summary>
        /// Determines whether the entry has expired at the given instant.
        /// </summary>
        public bool IsExpired( DateTimeOffset now ) => this.GetExpirationInstant() <= now;

        /// <summary>
        /// Gets the instant at which the entry expires, or <see langword="null"/> when it does not expire.
        /// </summary>
        public DateTimeOffset? GetExpirationInstant()
        {
            DateTimeOffset? expiration = null;

            if ( this.AbsoluteExpiration != null )
            {
                expiration = this.AbsoluteExpiration;
            }

            if ( this.SlidingExpiration != null )
            {
                var slidingExpiration = this._lastAccess + this.SlidingExpiration.Value;

                if ( expiration == null || slidingExpiration < expiration )
                {
                    expiration = slidingExpiration;
                }
            }

            return expiration;
        }

        public void NotifyEvicted( EvictionReason reason )
        {
            if ( Interlocked.Exchange( ref this._evicted, 1 ) != 0 )
            {
                return;
            }

            foreach ( var registration in this.PostEvictionCallbacks )
            {
                registration.EvictionCallback?.Invoke( this.Key, this.Value, reason, registration.State );
            }
        }

        public void Dispose()
        {
            if ( this.AbsoluteExpirationRelativeToNow != null )
            {
                var relativeExpiration = this._cache._timeProvider.GetUtcNow() + this.AbsoluteExpirationRelativeToNow.Value;

                if ( this.AbsoluteExpiration == null || relativeExpiration < this.AbsoluteExpiration )
                {
                    this.AbsoluteExpiration = relativeExpiration;
                }
            }

            this._cache.Add( this );
        }
    }
}
