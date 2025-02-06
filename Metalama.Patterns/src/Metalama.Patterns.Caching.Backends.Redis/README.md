![Metalama Logo](https://raw.githubusercontent.com/postsharp/Metalama/master/images/metalama-by-postsharp.svg)

## About

The `Metalama.Patterns.Caching.Backends.Redis` package complements the `Metalama.Patterns.Caching` package by adding a Redis-based cache implementation. It also synchronizes multiple in-memory caches, each typically deployed on a different node, through Redis Pub/Sub. This package is based on `StackExchange.Redis`.

## Key Features

* A Redis-based [CachingBackend](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-backends-cachingbackend) implementation. This allows you to use the `Metalama.Patterns.Caching` package with Redis instead of just a local memory cache.
* The invalidation of multiple local caches through Redis Pub/Sub.
* A garbage collection task for use when dependencies are enabled.

## Main Types

* [RedisCachingFactory](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-backends-redis-rediscachingfactory) provides extension methods to configure the Redis caching back-end, specifically:
    * [Redis](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-backends-redis-rediscachingfactory-redis) configures a Redis-backend.
    * [WithRedisSynchronization](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-backends-redis-rediscachingfactory-withredissynchronization) configures cache invalidation over Redis Pub/Sub.
    * [AddRedisCacheDependencyGarbageCollector](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-backends-redis-rediscachingfactory-addrediscachedependencygarbagecollector) adds the garbage collection service to the service collection. [CreateRedisCacheDependencyGarbageCollector](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-backends-redis-rediscachingfactory-createrediscachedependencygarbagecollector) returns a new instance of this service without adding it to any collection.

## Additional Documentation

* Conceptual documentation:
    * [Distributed caching with Redis](https://doc.postsharp.net/metalama/patterns/caching/redis)
    * [Synchronizing local in-memory caches with Redis Pub/Sub](https://doc.postsharp.net/metalama/patterns/caching/pubsub)
* [API Documentation](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-backends-redis)

## Related Packages

* `Metalama.Patterns.Caching` defines the cache API abstractions.
* `Metalama.Patterns.Caching.Aspects` provides an aspect-oriented API over `Metalama.Patterns.Caching` and is typically the entry point package.
* `Metalama.Patterns.Caching.Backends.Azure` implements distributed cache invalidation over Azure Service Bus instead of Redis Pub/Sub.
