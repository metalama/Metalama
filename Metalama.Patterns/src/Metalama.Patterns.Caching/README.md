![Metalama Logo](https://raw.githubusercontent.com/postsharp/Metalama/master/images/metalama-by-postsharp.svg)

## About

The `Metalama.Patterns.Caching` package is a front-end library for caching. It simplifies caching method results and invalidating the cached results.

This package is designed for use with the `Metalama.Patterns.Caching.Aspects` package, which offers Metalama-based aspects for caching, cache invalidation, and cache key generation. However, the `Metalama.Patterns.Caching` package does not rely on the Metalama aspect framework, and it can be used independently or with another aspect framework.

## Key Features

* Caching the return value of a method as a function of its arguments.
* Invalidation of cached method results, both directly and indirectly through cache dependencies.
* In-memory cache, Redis cache, Pub/Sub synchronized cache, L2 in-memory cache.
* Caching profiles for dynamically changing settings at runtime.
* Automatic reload of expired cached items.
* Cache key formatters through the `Flashtrace.Formatters` package.
* Value adapters for caching "special" types like streams or enumerables.
* Locking to prevent concurrent execution.

This package can interact with any cache implementation. The abstraction is provided by the [CachingBackend](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-backends-cachingbackend) class from the `Metalama.Patterns.Caching.Backend` package.

## Main Types

The primary types in this package are:

* [ICachingService](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-icachingservice) is the main interface.
* [CachingService](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-cachingservice) is the primary implementation of `ICachingService` and should generally not be directly used unless you are not using dependency injection.
* [CachingServiceFactory](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-building-cachingservicefactory) extends `IServiceCollection` and allows the addition of `ICachingService` to your application.
* [CachedMethodMetadata](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-cachedmethodmetadata) represents the metadata of a cached method. If you are not using an aspect framework, you must create an instance of this class for each cached method.
* [CacheKeyBuilder](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-formatters-cachekeybuilder) is the algorithm that generates the cache key based on the method metadata and its arguments.
* [IValueAdapter](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-valueadapters-ivalueadapter) is the abstraction for caching special types like streams or enumerables.

## Additional Documentation

* [Conceptual documentation](https://doc.postsharp.net/metalama/patterns/caching)
* [API documentation](https://doc.postsharp.net/metalama/api/metalama-patterns-caching)

## Related Packages

* `Metalama.Patterns.Caching.Aspects` provides an aspect-oriented API for the current package.
* `Metalama.Patterns.Caching.Backend` defines the [CachingBackend](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-backends-cachingbackend) abstraction and provides core implementations for in-memory and multi-layered caching.
* `Metalama.Patterns.Caching.Backends.Redis` offers the `CachingBackend` implementation for Redis.
* `Metalama.Patterns.Caching.Backends.Azure` implements pub/sub synchronization of distributed in-memory caches through Azure Message Bus.
* `Flashtrace.Formatters` provides an infrastructure for cache key formatting.

