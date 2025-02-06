// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace;
using JetBrains.Annotations;
using Metalama.Patterns.Caching.Building;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Metalama.Patterns.Caching.Backends.Redis;

/// <summary>
/// Extensions methods for <see cref="CachingBackendBuilder"/> and <see cref="IServiceCollection"/> that builds components
/// of the <c>Metalama.Patterns.Caching.Backends.Redis</c> namespace.
/// </summary>
[PublicAPI]
public static class RedisCachingFactory
{
    /// <summary>
    /// Builds a <see cref="CachingBackend"/> based on a Redis server given an existing <see cref="IConnectionMultiplexer"/>.
    /// </summary>
    public static RedisCachingBackendBuilder Redis(
        this CachingBackendBuilder builder,
        RedisCachingBackendConfiguration? configuration = null )
        => new( configuration, builder.ServiceProvider );

    /// <summary>
    /// Enhances an in-memory cache with a component that synchronizes several local, in-memory caches, using Redis Pub/Sub, given an <see cref="IConnectionMultiplexer"/>. 
    /// </summary>
    public static RedisCacheSynchronizerBuilder WithRedisSynchronization(
        this MemoryCachingBackendBuilder builder,
        RedisCacheSynchronizerConfiguration? configuration = null )
        => new( builder, configuration, builder.ServiceProvider );

    /// <summary>
    /// Adds a service (implementing <see cref="IHostedService"/>) that removes dependencies added when a <see cref="RedisCachingBackend"/> when items
    /// are expired or evicted from the cache. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is necessary to have at least one instance of the garbage collector active at any time, otherwise the cache will use storage to store dependencies
    /// that are no longer relevant, and will not be removed otherwise. It is allowed but redundant to have several concurrent instances of the garbage collector,
    /// but having a large number of them can hurt performance of the Redis server.
    /// </para>
    /// <para>
    /// If no instance of this service is running during some time,
    /// you must initiate full garbage collection by calling the &lt;see cref="RedisGarbageCollectionUtilities.PerformFullCollectionAsync(StackExchange.Redis.IConnectionMultiplexer,Metalama.Patterns.Caching.Backends.Redis.RedisCachingBackendConfiguration?,System.IServiceProvider?,System.Threading.CancellationToken)"/&gt; method.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddRedisCacheDependencyGarbageCollector(
        this IServiceCollection serviceCollection,
        RedisCachingBackendConfiguration configuration )
    {
        serviceCollection.AddFlashtrace( false );

        serviceCollection.AddHostedService<RedisCacheDependencyGarbageCollector>(
            serviceProvider => new RedisCacheDependencyGarbageCollector( configuration, serviceProvider ) );

        return serviceCollection;
    }

    public static IServiceCollection AddRedisCacheDependencyGarbageCollector(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, RedisCachingBackendConfiguration> configurationFactory )
    {
        serviceCollection.AddFlashtrace( false );

        serviceCollection.AddHostedService<RedisCacheDependencyGarbageCollector>(
            serviceProvider => new RedisCacheDependencyGarbageCollector( configurationFactory( serviceProvider ), serviceProvider ) );

        return serviceCollection;
    }

    /// <summary>
    /// Creates an instance of the garbage collector service described in <see cref="AddRedisCacheDependencyGarbageCollector(Microsoft.Extensions.DependencyInjection.IServiceCollection,Metalama.Patterns.Caching.Backends.Redis.RedisCachingBackendConfiguration)"/>.
    /// So start the instance, use <see cref="IHostedService.StartAsync"/>.
    /// </summary>
    public static IHostedService CreateRedisCacheDependencyGarbageCollector(
        RedisCachingBackendConfiguration configuration,
        IServiceProvider? serviceProvider = null )
        => new RedisCacheDependencyGarbageCollector( configuration, serviceProvider );
}