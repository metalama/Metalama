// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Formatters;
using System.Collections.Immutable;

namespace Metalama.Patterns.Caching;

/// <summary>
/// Front-end interface used by the caching aspects and for imperative cache invalidation.
/// </summary>
/// <remarks>
/// <para>This interface is the primary entry point for interacting with the caching system at run time.
/// It is consumed by the <c>CacheAttribute</c> aspect and provides imperative methods
/// for cache invalidation through the <see cref="CachingServiceExtensions"/> extension methods.</para>
/// <para>Instances are obtained either through dependency injection (when configured via
/// <see cref="Building.CachingServiceFactory.AddMetalamaCaching"/>) or through the
/// <see cref="CachingService.Default"/> static property (when dependency injection is disabled).</para>
/// </remarks>
/// <seealso cref="CachingService"/>
/// <seealso cref="CachingServiceExtensions"/>
/// <seealso cref="Building.CachingServiceFactory"/>
/// <seealso href="@caching-getting-started"/>
public interface ICachingService : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets the <see cref="FlashtraceSource"/> used for logging caching operations.
    /// </summary>
    FlashtraceSource Logger { get; }

    /// <summary>
    /// Gets the <see cref="ICacheKeyBuilder"/> used to generate cache keys from method arguments.
    /// </summary>
    ICacheKeyBuilder KeyBuilder { get; }

    /// <summary>
    /// Gets the collection of all <see cref="CachingBackend"/> instances used by this service across all profiles.
    /// </summary>
    ImmutableArray<CachingBackend> AllBackends { get; }

    /// <summary>
    /// Initializes the caching service. It is recommended to call this method from the start-up program
    /// sequence when the back-end involves a network or out-of-process service (e.g. Redis, Azure). If this
    /// method is not called, initialization will occur automatically upon the first call any
    /// cached method.
    /// </summary>
    Task InitializeAsync( CancellationToken cancellationToken = default );

    /// <summary>
    /// Gets a value from the cache or executes a synchronous method and caches the result.
    /// This method is called by the generated code of the <c>CacheAttribute</c> aspect.
    /// </summary>
    /// <typeparam name="TResult">The type of the cached value.</typeparam>
    /// <param name="metadata">Metadata about the cached method, or <c>null</c> if the cache registration
    /// has not been initialized yet (e.g., when a cached method is called from a static field initializer).
    /// When <c>null</c>, caching is bypassed and the original method is called directly.</param>
    /// <param name="instance">The instance on which the method is called, or <c>null</c> for static methods.</param>
    /// <param name="args">The method arguments.</param>
    /// <param name="func">A delegate that invokes the original method if the value is not in cache.</param>
    /// <param name="configuration">Optional configuration overriding the default settings.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The cached or computed result.</returns>
    TResult? GetFromCacheOrExecute<TResult>(
        CachedMethodMetadata? metadata,
        object? instance,
        object?[] args,
        Func<object?, object?[], object?> func,
        CacheItemConfiguration? configuration = null,
        CancellationToken cancellationToken = default );

    /// <summary>
    /// Gets a value from the cache or executes an asynchronous method returning <see cref="Task{TResult}"/> and caches the result.
    /// This method is called by the generated code of the <c>CacheAttribute</c> aspect.
    /// </summary>
    /// <typeparam name="TTaskResultType">The type of the task result.</typeparam>
    /// <param name="metadata">Metadata about the cached method, or <c>null</c> if the cache registration
    /// has not been initialized yet (e.g., when a cached method is called from a static field initializer).
    /// When <c>null</c>, caching is bypassed and the original method is called directly.</param>
    /// <param name="instance">The instance on which the method is called, or <c>null</c> for static methods.</param>
    /// <param name="args">The method arguments.</param>
    /// <param name="func">A delegate that invokes the original method if the value is not in cache.</param>
    /// <param name="configuration">Optional configuration overriding the default settings.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the cached or computed result.</returns>
    Task<TTaskResultType?> GetFromCacheOrExecuteTaskAsync<TTaskResultType>(
        CachedMethodMetadata? metadata,
        object? instance,
        object?[] args,
        Func<object?, object?[], CancellationToken, Task<object?>> func,
        CacheItemConfiguration? configuration = null,
        CancellationToken cancellationToken = default );

    /// <summary>
    /// Gets a value from the cache or executes an asynchronous method returning <see cref="ValueTask{TResult}"/> and caches the result.
    /// This method is called by the generated code of the <c>CacheAttribute</c> aspect.
    /// </summary>
    /// <typeparam name="TTaskResultType">The type of the value task result.</typeparam>
    /// <param name="metadata">Metadata about the cached method, or <c>null</c> if the cache registration
    /// has not been initialized yet (e.g., when a cached method is called from a static field initializer).
    /// When <c>null</c>, caching is bypassed and the original method is called directly.</param>
    /// <param name="instance">The instance on which the method is called, or <c>null</c> for static methods.</param>
    /// <param name="args">The method arguments.</param>
    /// <param name="func">A delegate that invokes the original method if the value is not in cache.</param>
    /// <param name="configuration">Optional configuration overriding the default settings.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A value task representing the cached or computed result.</returns>
    ValueTask<TTaskResultType?> GetFromCacheOrExecuteValueTaskAsync<TTaskResultType>(
        CachedMethodMetadata? metadata,
        object? instance,
        object?[] args,
        Func<object?, object?[], CancellationToken, ValueTask<object?>> func,
        CacheItemConfiguration? configuration = null,
        CancellationToken cancellationToken = default );
}