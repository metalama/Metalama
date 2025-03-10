// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using System;
using System.Diagnostics;

namespace Metalama.Backstage.Testing
{
    public class TestDateTimeProvider : IDateTimeProvider
    {
        private DateTime? _lastResetTime;
        private Stopwatch? _stopwatch;

        public DateTime UtcNow => this._lastResetTime?.AddMilliseconds( this._stopwatch?.ElapsedMilliseconds ?? 0 ) ?? DateTime.UtcNow;

        public void Stop() => this.Set( DateTime.UtcNow );

        public void Set( DateTime now, bool keepRunning = false )
        {
            if ( now.Kind != DateTimeKind.Utc )
            {
                throw new ArgumentException( "The date time must be in UTC.", nameof(now) );
            }

            this._lastResetTime = now;
            this._stopwatch = keepRunning ? Stopwatch.StartNew() : null;
        }

        public void AddTime( TimeSpan timeSpan ) => this.Set( this.UtcNow + timeSpan );
    }
}