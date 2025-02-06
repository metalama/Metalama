// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Formatters;
using System.Collections.Immutable;

namespace Metalama.Patterns.Caching;

/// <summary>
/// Front-end interface used by the caching aspects.
/// </summary>
public interface ICachingService : IAsyncDisposable, IDisposable
{
    FlashtraceSource Logger { get; }

    ICacheKeyBuilder KeyBuilder { get; }

    ImmutableArray<CachingBackend> AllBackends { get; }

    /// <summary>
    /// Initializes the caching service. It is recommended to call this method from the start-up program
    /// sequence when the back-end involves a network or out-of-process service (e.g. Redis, Azure). If this
    /// method is not called, initialization will occur automatically upon the first call any
    /// cached method.
    /// </summary>
    Task InitializeAsync( CancellationToken cancellationToken = default );

    TResult? GetFromCacheOrExecute<TResult>(
        CachedMethodMetadata metadata,
        object? instance,
        object?[] args,
        Func<object?, object?[], object?> func,
        CacheItemConfiguration? configuration = null,
        CancellationToken cancellationToken = default );

    Task<TTaskResultType?> GetFromCacheOrExecuteTaskAsync<TTaskResultType>(
        CachedMethodMetadata metadata,
        object? instance,
        object?[] args,
        Func<object?, object?[], CancellationToken, Task<object?>> func,
        CacheItemConfiguration? configuration = null,
        CancellationToken cancellationToken = default );

    ValueTask<TTaskResultType?> GetFromCacheOrExecuteValueTaskAsync<TTaskResultType>(
        CachedMethodMetadata metadata,
        object? instance,
        object?[] args,
        Func<object?, object?[], CancellationToken, ValueTask<object?>> func,
        CacheItemConfiguration? configuration = null,
        CancellationToken cancellationToken = default );
}