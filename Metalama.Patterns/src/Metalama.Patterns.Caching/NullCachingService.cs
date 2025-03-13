// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace;
using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Formatters;
using System.Collections.Immutable;

namespace Metalama.Patterns.Caching;

[PublicAPI]
public sealed class NullCachingService : ICachingService
{
    public static NullCachingService Instance { get; } = new();

    private NullCachingService() { }

    public FlashtraceSource Logger { get; } = FlashtraceSource.Null;

    public ICacheKeyBuilder KeyBuilder { get; } = new NullKeyBuilder();

    public ImmutableArray<CachingBackend> AllBackends { get; } = ImmutableArray<CachingBackend>.Empty;

    public Task InitializeAsync( CancellationToken cancellationToken = default ) => Task.CompletedTask;

    public TResult? GetFromCacheOrExecute<TResult>(
        CachedMethodMetadata metadata,
        object? instance,
        object?[] args,
        Func<object?, object?[], object?> func,
        CacheItemConfiguration? configuration = null,
        CancellationToken cancellationToken = default )
        => (TResult?) func( instance, args );

    public async Task<TTaskResultType?> GetFromCacheOrExecuteTaskAsync<TTaskResultType>(
        CachedMethodMetadata metadata,
        object? instance,
        object?[] args,
        Func<object?, object?[], CancellationToken, Task<object?>> func,
        CacheItemConfiguration? configuration = null,
        CancellationToken cancellationToken = default )
        => (TTaskResultType?) await func( instance, args, cancellationToken );

    public async ValueTask<TTaskResultType?> GetFromCacheOrExecuteValueTaskAsync<TTaskResultType>(
        CachedMethodMetadata metadata,
        object? instance,
        object?[] args,
        Func<object?, object?[], CancellationToken, ValueTask<object?>> func,
        CacheItemConfiguration? configuration = null,
        CancellationToken cancellationToken = default )
        => (TTaskResultType?) await func( instance, args, cancellationToken );

    private sealed class NullKeyBuilder : ICacheKeyBuilder
    {
        public string BuildMethodKey( CachedMethodMetadata metadata, object? instance, IList<object?> arguments ) => "";

        public string BuildDependencyKey( object o ) => "";
    }

    public void Dispose() { }

    public ValueTask DisposeAsync() => default;
}