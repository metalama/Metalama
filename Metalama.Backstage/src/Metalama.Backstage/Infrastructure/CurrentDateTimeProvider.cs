// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Timers;

namespace Metalama.Backstage.Infrastructure
{
    /// <summary>
    /// Provides current date and time using <see cref="DateTime.UtcNow" />.
    /// </summary>
    internal sealed class CurrentDateTimeProvider : IDateTimeProvider
    {
        private readonly Timer _timer = new();

        public CurrentDateTimeProvider()
        {
            this._timer.Elapsed += this.OnTimerElapsed;
            this.ScheduleNextMidnight();
        }

        private void ScheduleNextMidnight()
        {
            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays( 1 ).Date;
            var timeUntilMidnight = nextMidnight - now;

            this._timer.Interval = timeUntilMidnight.TotalMilliseconds;
            this._timer.AutoReset = false; // Ensure it fires only once
            this._timer.Start();
        }

        private void OnTimerElapsed( object? sender, ElapsedEventArgs e )
        {
            this.DateChanged?.Invoke();
            this.ScheduleNextMidnight(); 
        }

        /// <summary>
        /// Gets current date and time using <see cref="DateTime.UtcNow" />.
        /// </summary>
        public DateTime UtcNow => DateTime.UtcNow;

        public event Action? DateChanged;

        public void Dispose() => this._timer.Dispose();
    }
}