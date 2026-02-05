// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.TestHelpers
{
    public static class TaskExtensions
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds( 30 );

        public static async Task<bool> WithTimeout( this Task task, TimeSpan delay )
        {
            return await Task.WhenAny( task, Task.Delay( delay ) ) == task;
        }

        /// <summary>
        /// Waits for a task with a timeout, throwing if the timeout is exceeded.
        /// Compatible with .NET Framework 4.7.2.
        /// </summary>
        public static async Task WaitWithTimeoutAsync( this Task task, string message = "Timeout exceeded.", TimeSpan? timeout = null )
        {
            var actualTimeout = timeout ?? _defaultTimeout;

            if ( !await task.WithTimeout( actualTimeout ) )
            {
                throw new TimeoutException( message );
            }
        }

        /// <summary>
        /// Waits for a task with a timeout and returns the result.
        /// Compatible with .NET Framework 4.7.2.
        /// </summary>
        public static async Task<T> WaitWithTimeoutAsync<T>( this Task<T> task, string message = "Timeout exceeded.", TimeSpan? timeout = null )
        {
            var actualTimeout = timeout ?? _defaultTimeout;

            if ( await Task.WhenAny( task, Task.Delay( actualTimeout ) ) != task )
            {
                throw new TimeoutException( message );
            }

            return await task;
        }
    }
}