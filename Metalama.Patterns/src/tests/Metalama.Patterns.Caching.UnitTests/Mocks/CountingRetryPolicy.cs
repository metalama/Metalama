// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Resilience;

namespace Metalama.Patterns.Caching.Tests.Mocks;

/// <summary>
/// A test retry policy that counts the number of retry attempts.
/// </summary>
internal sealed class CountingRetryPolicy : IRetryPolicy
{
    public int MaxAttempts { get; set; } = 3;

    private int _tryCount;

    public int TryCount => this._tryCount;

    public ValueTask<bool> TryAsync(
        OperationKind kind,
        int attempt,
        Exception? exception,
        ref object? state,
        CancellationToken cancellationToken = default )
    {
        Interlocked.Increment( ref this._tryCount );

        if ( attempt >= this.MaxAttempts )
        {
            throw new CachingException( $"Max attempts ({this.MaxAttempts}) exceeded" );
        }

        return new ValueTask<bool>( true );
    }
}
