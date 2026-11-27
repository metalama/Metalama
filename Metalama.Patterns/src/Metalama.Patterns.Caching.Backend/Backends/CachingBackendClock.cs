// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// The clock that a <see cref="CachingBackend"/> uses to time the invalidations of its keys, and that the caching service
/// uses to time the start of the computation of a cache item.
/// </summary>
/// <remarks>
/// <para>
/// The caching service reads the clock before it computes the value of a cache item, and passes the time to
/// <see cref="CachingBackend.SetItem"/>. The backend stores the item only when neither its key nor any of
/// its dependencies has been invalidated since that time. The times of the invalidations and of the computations must
/// therefore be read from the same clock, or from clocks whose offset is at most <see cref="Tolerance"/>.
/// </para>
/// <para>
/// The default clock of a backend reads the <see cref="System.TimeProvider"/> of the service provider. A distributed backend
/// can use the clock of its server instead.
/// </para>
/// </remarks>
/// <seealso cref="CachingBackend.Clock"/>
[PublicAPI]
public abstract class CachingBackendClock
{
    /// <summary>
    /// Creates a <see cref="CachingBackendClock"/> that reads a <see cref="System.TimeProvider"/>.
    /// </summary>
    /// <param name="timeProvider">The <see cref="System.TimeProvider"/>.</param>
    /// <param name="tolerance">The maximal offset between this clock and the clocks of the other machines that invalidate the same backend.</param>
    /// <returns>A <see cref="CachingBackendClock"/>.</returns>
    public static CachingBackendClock FromTimeProvider( TimeProvider timeProvider, TimeSpan tolerance = default )
    {
        if ( timeProvider == null )
        {
            throw new ArgumentNullException( nameof(timeProvider) );
        }

        if ( tolerance < TimeSpan.Zero )
        {
            throw new ArgumentOutOfRangeException( nameof(tolerance), "The tolerance cannot be negative." );
        }

        return new TimeProviderClock( timeProvider, tolerance );
    }

    /// <summary>
    /// Gets the maximal offset between this clock and the clocks that time the invalidations of the backend. An invalidation
    /// that is timed up to this duration before the start of a computation is considered to have happened during the
    /// computation. The default value is zero.
    /// </summary>
    public virtual TimeSpan Tolerance => TimeSpan.Zero;

    /// <summary>
    /// Gets the current time.
    /// </summary>
    /// <returns>The current time.</returns>
    public abstract DateTimeOffset GetCurrentTime();

    /// <summary>
    /// Asynchronously gets the current time. The default implementation calls <see cref="GetCurrentTime"/>.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The current time.</returns>
    public virtual ValueTask<DateTimeOffset> GetCurrentTimeAsync( CancellationToken cancellationToken = default )
    {
        cancellationToken.ThrowIfCancellationRequested();

        return new ValueTask<DateTimeOffset>( this.GetCurrentTime() );
    }

    /// <summary>
    /// A clock that reads a <see cref="System.TimeProvider"/> with the resolution of its high-frequency timestamp.
    /// </summary>
    /// <remarks>
    /// On .NET Framework, the wall clock has a resolution of about fifteen milliseconds, so an invalidation and the start of
    /// the computation that follows it would often be timed at the same instant, and the computed item would be rejected.
    /// The clock therefore adds the elapsed time of the high-frequency timestamp to a reading of the wall clock, which it
    /// reads again every minute so that it does not drift away from the wall clock. The returned time never decreases.
    /// </remarks>
    private sealed class TimeProviderClock : CachingBackendClock
    {
        private static readonly TimeSpan _anchorLifetime = TimeSpan.FromMinutes( 1 );

        private readonly TimeProvider _timeProvider;
        private Anchor _anchor;
        private long _lastTicks;

        public TimeProviderClock( TimeProvider timeProvider, TimeSpan tolerance )
        {
            this._timeProvider = timeProvider;
            this.Tolerance = tolerance;
            this._anchor = new Anchor( timeProvider.GetUtcNow().UtcTicks, timeProvider.GetTimestamp() );
        }

        public override TimeSpan Tolerance { get; }

        public override DateTimeOffset GetCurrentTime()
        {
            var anchor = Volatile.Read( ref this._anchor );
            var elapsed = this._timeProvider.GetElapsedTime( anchor.Timestamp );

            long ticks;

            if ( elapsed >= _anchorLifetime || elapsed < TimeSpan.Zero )
            {
                anchor = new Anchor( this._timeProvider.GetUtcNow().UtcTicks, this._timeProvider.GetTimestamp() );
                Volatile.Write( ref this._anchor, anchor );
                ticks = anchor.UtcTicks;
            }
            else
            {
                ticks = anchor.UtcTicks + elapsed.Ticks;
            }

            // The time never decreases, even when the wall clock is set back.
            while ( true )
            {
                var lastTicks = Volatile.Read( ref this._lastTicks );

                if ( ticks <= lastTicks )
                {
                    return new DateTimeOffset( lastTicks, TimeSpan.Zero );
                }

                if ( Interlocked.CompareExchange( ref this._lastTicks, ticks, lastTicks ) == lastTicks )
                {
                    return new DateTimeOffset( ticks, TimeSpan.Zero );
                }
            }
        }

        private sealed record Anchor( long UtcTicks, long Timestamp );
    }
}
