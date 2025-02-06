![Metalama Logo](https://raw.githubusercontent.com/postsharp/Metalama/master/images/metalama-by-postsharp.svg)

## About

The `Metalama.Patterns.Caching.Aspects` package provides an aspect-oriented API that enables caching of method results as a function of their arguments. It also allows for cache item invalidation, cache key construction, and dependency management. This API is built on the Metalama aspect framework. The caching abstractions and functionalities are defined in the `Metalama.Patterns.Caching` family of packages.

## Key Features

* Caching the return value of a method as a function of its argument.
* Automatic invalidation of a cached method result when an update method is executed.
* Dependency collection during the execution of the cached method, and automatic addition of dependencies to the cache item.
* Automatic and reliable generation of cache keys.

## Main Types

* [CacheAttribute](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-aspects-cacheattribute) is the primary caching aspect.
* [InvalidateCacheAttribute](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-aspects-invalidatecacheattribute) is an aspect to be applied to the Update method, which causes the Get method to be removed from the cache for matching arguments.
* [CacheKeyAttribute](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-aspects-cachekeyattribute) is an aspect to be applied to the fields or properties of a type that can be used as a parameter for a cached method. It designates the fields and properties that should be a part of the cache key.

## Additional Documentation

* [Reference documentation](https://doc.postsharp.net/metalama/patterns/caching)
* [API documentation](https://doc.postsharp.net/metalama/api/metalama-patterns-caching-aspects)

## Related Packages

* `Metalama.Patterns.Caching` is the object-oriented API for which the current package provides aspects.
* `Metalama.Patterns.Memoization` offers a simple, high-performance alternative to caching, working only with read-only properties and parameterless methods.
