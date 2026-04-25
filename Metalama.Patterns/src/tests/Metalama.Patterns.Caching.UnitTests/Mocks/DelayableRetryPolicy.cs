// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Resilience;

namespace Metalama.Patterns.Caching.Tests.Mocks;

/// <summary>
/// A test retry policy that can inject delays during retry attempts.
/// </summary>
internal sealed class DelayableRetryPolicy : IRetryPolicy
{
    public int MaxAttempts { get; set; } = 3;

    public TaskCompletionSource<bool>? DelayTaskSource { get; set; }

    public int DelayOnAttempt { get; set; } = 1;

    public ValueTask<bool> TryAsync(
        OperationKind kind,
        int attempt,
        Exception? exception,
        ref object? state,
        CancellationToken cancellationToken = default )
        => this.TryAsyncImpl( attempt, cancellationToken );

    private async ValueTask<bool> TryAsyncImpl( int attempt, CancellationToken cancellationToken )
    {
        if ( attempt >= this.MaxAttempts )
        {
            throw new CachingException( $"Max attempts ({this.MaxAttempts}) exceeded" );
        }

        if ( attempt >= this.DelayOnAttempt && this.DelayTaskSource != null )
        {
            await this.DelayTaskSource.Task;
            cancellationToken.ThrowIfCancellationRequested();
        }

        return true;
    }
}