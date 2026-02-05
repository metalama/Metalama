// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Resilience;
using System.Collections.Concurrent;

namespace Metalama.Patterns.Caching.Tests.Implementation;

public sealed partial class BackgroundTaskSchedulerTests
{
    /// <summary>
    /// A mock retry policy for testing retry behavior.
    /// </summary>
    private sealed class MockRetryPolicy : IRetryPolicy
    {
        public int MaxAttempts { get; set; } = 3;

        public TaskCompletionSource<bool>? DelayTcs { get; set; }

        public ConcurrentBag<int> AttemptsMade { get; } = new();

        public ValueTask<bool> TryAsync(
            OperationKind kind,
            int attempt,
            Exception? exception,
            ref object? state,
            CancellationToken cancellationToken = default )
            => this.TryAsyncImpl( attempt, cancellationToken );

        private async ValueTask<bool> TryAsyncImpl( int attempt, CancellationToken cancellationToken )
        {
            this.AttemptsMade.Add( attempt );

            if ( attempt >= this.MaxAttempts )
            {
                throw new CachingException( $"Max attempts ({this.MaxAttempts}) exceeded" );
            }

            if ( attempt > 0 && this.DelayTcs != null )
            {
                await this.DelayTcs.Task;
                cancellationToken.ThrowIfCancellationRequested();
            }

            return true;
        }
    }
}