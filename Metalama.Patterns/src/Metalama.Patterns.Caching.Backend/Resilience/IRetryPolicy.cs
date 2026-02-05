// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Resilience;

/// <summary>
/// Defines a <see cref="TryAsync"/> method that determines if a Redis transaction or operation should be retried if it fails.
/// The default implementation is <see cref="RetryPolicy"/>.
/// </summary>
[PublicAPI]
public interface IRetryPolicy
{
    /// <summary>
    /// Determines if a Redis transaction should be tried, and throws a <see cref="CachingException"/> if it cannot.
    /// </summary>
    /// <param name="kind">The transaction kind.</param>
    /// <param name="attempt">The index of the attempt. <c>0</c> indicates the first attempt, i.e. not a retry.</param>
    /// <param name="exception">The <see cref="Exception"/> of the previous attempt, if any.</param>
    /// <param name="state">A to an optional state of the operation, optionally set by the <see cref="IRetryPolicy"/> implementation and
    /// opaque to the caller.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>Always <c>true</c>. Must throw an exception if it cannot retry.</returns>
    ValueTask<bool> TryAsync( OperationKind kind, int attempt, Exception? exception, ref object? state, CancellationToken cancellationToken = default );
}