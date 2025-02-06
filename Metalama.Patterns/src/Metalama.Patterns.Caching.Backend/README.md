![Metalama Logo](https://raw.githubusercontent.com/postsharp/Metalama/master/images/metalama-by-postsharp.svg)

## About

The `Metalama.Patterns.Caching.Backend` package provides an abstraction for a caching back-end, along with core implementations. The main type is the [CachingBackend](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-backends-cachingbackend) class. The back-end implementations are utilized by the front-end package, `Metalama.Patterns.Caching`.

## Key Features

* In-memory caching, with or without support for dependencies.
* L2 in-memory cache, typically used in front of a remote cache.
* Abstractions to support pub/sub invalidation of a network of in-memory caches.

## Main Types

* [CachingBackend](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-backends-cachingbackend) is the primary abstract class.
* [CachingBackendBuilder](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-building-cachingbackendbuilder), its derived classes, and the extension methods in [ConcreteCachingBackendBuilder](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-building-cachingbackendfactory), facilitate the instantiation of a `CachingBackend`. Typically, you'd receive a `CachingBackendBuilder` when initializing `ICachingService` from the `Metalama.Patterns.Caching` package.

## Additional Documentation

* [Conceptual documentation](https://doc.postsharp.net/metalama/patterns/caching)
* [API documentation](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-backends)

## Related Packages

* `Metalama.Patterns.Caching` provides the front-end API for the caching library.
* `Metalama.Patterns.Caching.Aspects` offers an aspect-oriented API to `Metalama.Patterns.Caching`.
* `Metalama.Patterns.Caching.Backends.Redis` provides the `CachingBackend` implementation for Redis.
* `Metalama.Patterns.Caching.Backends.Azure` implements the pub/sub synchronization of distributed in-memory caches through Azure Message Bus.
