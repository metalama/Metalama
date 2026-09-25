# Caching Implementation

This document describes the architecture and implementation of the Metalama caching subsystem.

## Overview

The caching system is organized across three projects:

| Project | Purpose |
|---------|---------|
| `Metalama.Patterns.Caching` | High-level API: `CachingService`, profiles, key builders, value adapters |
| `Metalama.Patterns.Caching.Backend` | Physical cache storage, enhancers, serialization, synchronization |
| `Metalama.Patterns.Caching.Aspects` | AOP integration: `CacheAttribute`, `InvalidateCacheAttribute` |

## Backend Architecture

The backend system uses a **decorator/chain-of-responsibility pattern** where enhancers wrap other backends.

### Base Classes

```
CachingBackend (abstract)
    └── CachingBackendEnhancer (decorator base)
            └── LayeredCachingBackendEnhancer
            └── NonBlockingCachingBackendEnhancer
            └── CacheSynchronizer (abstract)
    └── MemoryCachingBackend
    └── NullCachingBackend
```

### `CachingBackend`

Abstract base class providing:
- Sync/async API pairs: `SetItem()`/`SetItemAsync()`, `GetItem()`/`GetItemAsync()`, etc.
- Initialization state machine: Default → Initializing → Initialized / Failed
- Events: `ItemRemoved`, `DependencyInvalidated`
- Feature discovery via `SupportedFeatures` property

### `CachingBackendEnhancer`

Base class for enhancers that wrap another `CachingBackend`. Forms chains:

```
NonBlockingEnhancer → LayeredEnhancer → MemoryBackend
```

Each enhancer can intercept operations before delegating to the underlying backend.

### Built-in Backends

| Backend | Description |
|---------|-------------|
| `MemoryCachingBackend` | Uses `Microsoft.Extensions.Caching.Memory.IMemoryCache` |
| `NullCachingBackend` | No-op backend for disabling caching |
| `UninitializedCachingBackend` | Placeholder for uninitialized state |

### Premium Backends

Additional backends are available in the `Metalama.Premium` repository:

| Backend | Description |
|---------|-------------|
| `RedisCachingBackend` | Redis-based distributed caching with dependency support |
| `AzureCacheSynchronizer` | Cache synchronization via Azure Service Bus |

## Key Enhancers

### `LayeredCachingBackendEnhancer`

Adds a fast local L1 (in-memory) cache in front of a slower L2 (remote) backend.

- Synchronizes L1 and L2 through events
- When L2 removes an item, L1 is invalidated
- Configurable transition period prevents L1/L2 inconsistencies

### `NonBlockingCachingBackendEnhancer`

Enqueues all write operations as background tasks:
- `SetItem`, `RemoveItem`, `InvalidateDependency`, `Clear`
- Returns immediately to caller
- Uses `BackgroundTaskScheduler` for execution

Sets `Blocking = false` in features to signal non-blocking behavior.

### `CacheSynchronizer`

Abstract base for multi-instance cache synchronization:
- Publishes cache invalidations over pub/sub channel
- Receives messages from other instances
- Uses `BackgroundTaskScheduler` (sequential mode) to serialize publish operations

## Background Task Scheduling

`BackgroundTaskScheduler` provides infrastructure for background operations.

### Execution Modes

| Mode | Description |
|------|-------------|
| Parallel (default) | Up to `maxConcurrency` tasks (default 50) run concurrently |
| Sequential | Tasks execute strictly in order |

### Concurrency Control

- Uses `SemaphoreSlim` for throttling
- Queued tasks wait for semaphore slot availability

### Overload Detection

- Tracks queued task count
- When queue exceeds `overloadThreshold` (default 500 above maxConcurrency), enters overloaded state
- `IsOverloaded` property and `IsOverloadedChanged` event signal condition

### Retry Support

- Integrates with `IRetryPolicy` for automatic retry
- Releases semaphore during retry delays

### Key Methods

| Method | Purpose |
|--------|---------|
| `EnqueueBackgroundTask()` | Queue a task for background execution |
| `WhenBackgroundTasksCompleted()` | Wait for all queued tasks to complete |
| `Cancel()` | Cancel all pending tasks |

## Substitutable Dependencies

A caching backend has three dependencies that another implementation can replace: the clock, the object that dispatches work items, and the memory cache. Each has a production implementation that is used when the service provider supplies none, so none of them is ever absent, and the behaviour with the defaults is the production behaviour.

| Dependency | Type | Default | Supplied through |
|------------|------|---------|------------------|
| Clock | `System.TimeProvider` | `TimeProvider.System` | The service provider of the backend |
| Work-item dispatch | `IWorkItemDispatcher` | `ThreadPoolWorkItemDispatcher.Instance` | The service provider of the backend |
| Memory cache | `Microsoft.Extensions.Caching.Memory.IMemoryCache` | A new `MemoryCache` with default options | The backend builder, or the service provider |

These three are dependencies, not test hooks. A test hook, such as `ITestSynchronizationProvider` in `AwaitableEvent` and `MemoryCachingBackend`, or `IBackgroundTaskSchedulerObserver` in `BackgroundTaskScheduler`, is called by the product to notify a test, has no production meaning, and does nothing when it is absent. Time, execution and storage are dependencies of the backend, so they are modelled in the same way as `IRetryPolicy`.

The `Metalama.Patterns.Caching.TestHelpers` package supplies an implementation of each one, and `FakeCachingServices` registers the three of them in a single service provider. See the README of that package.

### Clock

`CachingBackend.TimeProvider` is the clock of the backend. It is `TimeProvider.System` unless the service provider supplies another `TimeProvider`. The property is `protected internal`, so a class that derives from `CachingBackend` reads the clock through it instead of reading `DateTime.UtcNow`.

Two components read the clock:

- `LayeredCachingBackendEnhancer` stamps the items that it writes to the L2 layer, and it computes the expiration of the tombstone that it writes into the L1 layer when an item is removed. The transition period of that tombstone is one minute.
- `MaterializedCacheItem` converts a relative absolute expiration into an absolute one, and back. It receives the `TimeProvider` as a constructor argument, because it has no service provider of its own.

### Work-item dispatch

`IWorkItemDispatcher` declares one method, `Dispatch`, which queues a work item. Every event and every background operation of a backend goes through it:

| Component | What it dispatches |
|-----------|--------------------|
| `CachingBackend.RaiseEvent` | The `ItemRemoved` and `DependencyInvalidated` events |
| `BackgroundTaskScheduler` | The background tasks that it executes |
| `AwaitableEvent` | The continuation of an asynchronous wait, when the captured `TaskScheduler` is the default one |

`CachingBackend.WorkItemDispatcher` exposes the dispatcher to derived classes. It is `ThreadPoolWorkItemDispatcher` unless the service provider supplies another `IWorkItemDispatcher`. `ThreadPoolWorkItemDispatcher` is a singleton, reached through `ThreadPoolWorkItemDispatcher.Instance`. It calls `ThreadPool.QueueUserWorkItem` when the execution context flows, and `ThreadPool.UnsafeQueueUserWorkItem` when it does not.

The interface only queues. The ability to wait for the completion of the pending work items belongs to the implementation that a test substitutes, not to the interface, because the product never waits for a work item that it has queued.

### Memory cache

`MemoryCachingBackend` stores its entries in an `IMemoryCache`. It takes the first of the following that is available:

1. The instance given to `MemoryCachingBackendBuilder.WithMemoryCache` or `LayeredCachingBackendBuilder.WithMemoryCache`, or the `MemoryCache` that `WithMemoryCacheOptions` builds from the given options.
2. The `IMemoryCache` of the service provider.
3. A new `MemoryCache` with default options.

`LayeredCachingBackendBuilder.WithMemoryCache` supplies the memory cache of the L1 layer. The L2 layer is the underlying backend and has a memory cache only if it is itself a `MemoryCachingBackend`.

The backend disposes the `IMemoryCache` only when it owns it: when it created it, or when `WithMemoryCacheOptions` created it. An instance given to `WithMemoryCache`, and the `IMemoryCache` of the service provider, belong to the caller. When the backend is disposed, it removes its own entries from such a cache and leaves the cache usable.

`IMemoryCache` declares no operation that removes every entry, although `MemoryCache` has one. `IClearableMemoryCache` is an `IMemoryCache` that declares `Clear` and `Compact`. `MemoryCachingBackend` reports the `Clear` feature when its `IMemoryCache` is a `MemoryCache` or implements `IClearableMemoryCache`. A call to `Clear` on a backend whose `IMemoryCache` is neither throws `NotSupportedException`.

### Concurrency in `MemoryCachingBackend`

`MemoryCachingBackend` keeps a backward dependency index: for each dependency key, the set of the keys of the items that declare it in their forward dependencies (`CacheItem.Dependencies`). The index is owned by the backend, in a `ConcurrentDictionary`, and is not stored in the `IMemoryCache`. The following rules keep the items and the index consistent when operations run concurrently.

- Every operation that changes the item of a key, or the registrations of that key in the index, holds the lock of that key. This applies to `SetItem`, `RemoveItem`, each step of an invalidation, each step of `Clear`, and the post-eviction callback. The lock of a key does not depend on the stored value, so two operations on the same key always exclude each other.
- A dependency set is locked only for one change or one copy. No thread acquires a key lock or another set lock while it holds a set lock, so the locks cannot form a cycle.
- A set that becomes empty is marked as removed and removed from the index as that exact instance. A thread that has read a removed set retries with the current set, so no key is registered in a set that no lookup reaches.
- The serializer and the size calculator run before any lock is acquired and before any state changes. An exception leaves the backend unchanged, and a size calculator or serializer that uses the backend cannot deadlock with it.
- Each stored entry carries a state object (`MemoryCacheItem.State`) that records whether the backend has already removed, replaced or evicted the entry. A post-eviction callback runs on the thread pool, possibly after a newer value has been stored under the same key. It does nothing for an entry that has already been handled, it unregisters only the dependencies that the newer value does not declare, and it raises `ItemRemoved` only when the key has no current value.
- An invalidation copies the dependency set, then processes each key under its lock. It removes a value only when the value still declares the invalidated dependency. It recurses into the dependents of every key, including a key whose item is absent, and it removes a key from the invalidated set only after its dependents have been processed. A concurrent invalidation of the same dependency therefore walks the same dependents before it returns. A set of visited keys stops the recursion on a cyclic dependency graph.
- When an item is removed or evicted while other items depend on its key, its dependencies stay registered until the last dependent is gone. An invalidation of one of these dependencies still reaches the dependents.
- `Clear` removes only the items of the current instance, one key at a time, except with `ClearCacheOptions.Compact` on a cache that the backend owns, where it compacts the cache.

The replacement value (tombstone) that `LayeredCachingBackendEnhancer` stores in its local layer when the remote layer is not blocking is stored even when the key is absent, with the priority `NeverRemove`, a size of zero, and an expiration converted to a duration with the clock of the backend.

The synchronization points of `MemoryCachingBackend` are `RemoveItemImpl:ItemLocked`, `InvalidateDependencyImpl:DependencyLocked`, `InvalidateDependencyImpl:DependentsCopied`, `AddBackwardDependency:DependencySetRead`, `RemoveBackwardDependency:DependencySetRead` and `ClearCore:ClearRequested`. The other interleavings are forced in tests through a decorator of `IMemoryCache` (`InterceptingMemoryCache` in the unit tests), which blocks a thread at a chosen call of the cache.

### Substitution in tests

Substitution is opt-in per test class. A test class that needs it creates a `FakeCachingServices` and passes its `ServiceProvider` to the backend under test. Every other test class keeps running against the real clock, the real thread pool and `MemoryCache`, and its behaviour is unchanged.

Three test suites stay on the real thread pool on purpose, because they exist to exercise a real and contended one:

| Test suite | Reason |
|------------|--------|
| `AwaitableEventRaceTests` | Reproduces interleavings of `AwaitableEvent` between two real threads |
| `BackgroundTaskSchedulerEdgeCaseTests` | Exercises the concurrency limit and the overload detection of `BackgroundTaskScheduler` |
| `AwaitableEventHangDiagnostic` | A load test that repeats the enqueue-then-await handshake a large number of times, under processor saturation |

`BaseCacheBackendTests` also stays on the real clock. It is shared with the Redis and Azure backends of `Metalama.Premium`, which run against a network and a real clock, so a fake clock cannot drive them.

## Serialization

Two-layer serialization system:

### `ICachingSerializer`

Interface for cache value serialization. Implementations:
- `JsonCachingSerializer`: JSON with type names using `System.Text.Json`

### `CacheItemSerializer`

Wraps `ICachingSerializer`, adding metadata marker byte:
- `0`: Standard `CacheItem`
- `1`: `MaterializedCacheItem` (pre-computed derivatives)

## Locking Strategies

`ILockingStrategy` synchronizes concurrent execution of cached methods.

### Implementations

| Strategy | Description |
|----------|-------------|
| `NullLockingStrategy` | No locking (allows concurrent execution) - default |
| `LocalLockingStrategy` | Process-local locks via `ConcurrentDictionary<string, Lock>` |

### `LocalLockingStrategy`

- Named locks (one per unique cache key)
- Supports sync/async acquisition with timeout and cancellation
- Automatic cleanup via reference counting

### Locking Flow in `CachingFrontend`

1. Try non-blocking lock acquire (fast path)
2. If locked, wait with timeout
3. Re-check cache after acquiring lock
4. Fall back to lock timeout handler if timeout exceeded

## Cache Items

### `CacheItem`

Record holding:
- `Value`: The cached object
- `Dependencies`: `ImmutableArray<string>` of dependency keys
- `Configuration`: `ICacheItemConfiguration` with TTL, eviction priority

### `MaterializedCacheItem`

Subclass with pre-computed derived values for optimized serialization.

## Resilience Infrastructure

Located in `Resilience` namespace.

### `RetryPolicy`

Exponential backoff with jitter:
- `delay = baseDelay × multiplier^(attempt-1) + jitter`
- Defaults: 25ms base, 1.2 multiplier, 2s max, 0.2 jitter factor, 5 max attempts

### `IExceptionHandlingPolicy`

Determines recovery action: `Continue`, `Retry`, `Abort`

### `DefaultExceptionHandlingPolicy`

- Retries on transient failures (connection issues)
- Aborts on permanent failures (serialization errors)

## Feature Discovery

`CachingBackendFeatures` bitmask:

| Feature | Description |
|---------|-------------|
| `Clear` | `Clear()` method supported |
| `Events` | `ItemRemoved`/`DependencyInvalidated` events raised |
| `Blocking` | Write operations complete synchronously |
| `Dependencies` | Dependency-based invalidation supported |
| `ContainsDependency` | `ContainsDependency()` method supported |

Enhancers delegate to underlying backend features (except `NonBlockingCachingBackendEnhancer` which overrides `Blocking` to false).

## Integration Points

| Component | Purpose |
|-----------|---------|
| `CachingFrontend` | Runtime cache lookup/store orchestration with locking |
| `CachingService` | Service container managing profiles, backends, factories |
| `CacheAttribute` | Compile-time weaving that calls `CachingFrontend.GetOrAdd()` |
| `CachingProfile` | Per-profile configuration (backend, locking strategy, timeouts) |

## Key Files Reference

| Component | Location |
|-----------|----------|
| `CachingBackend` | `Metalama.Patterns.Caching.Backend/CachingBackend.cs` |
| `BackgroundTaskScheduler` | `Metalama.Patterns.Caching.Backend/Implementation/BackgroundTaskScheduler.cs` |
| `AwaitableEvent` | `Metalama.Patterns.Caching.Backend/Implementation/AwaitableEvent.cs` |
| `CacheSynchronizer` | `Metalama.Patterns.Caching.Backend/Implementation/CacheSynchronizer.cs` |
| `LocalLockingStrategy` | `Metalama.Patterns.Caching.Backend/Locking/LocalLockingStrategy.cs` |
| `LayeredCachingBackendEnhancer` | `Metalama.Patterns.Caching.Backend/Backends/LayeredCachingBackendEnhancer.cs` |
| `NonBlockingCachingBackendEnhancer` | `Metalama.Patterns.Caching.Backend/Backends/NonBlockingCachingBackendEnhancer.cs` |
| `JsonCachingSerializer` | `Metalama.Patterns.Caching.Backend/Serializers/JsonCachingSerializer.cs` |
| `CacheItemSerializer` | `Metalama.Patterns.Caching.Backend/Serializers/CacheItemSerializer.cs` |
| `IWorkItemDispatcher` | `Metalama.Patterns.Caching.Backend/Implementation/IWorkItemDispatcher.cs` |
| `ThreadPoolWorkItemDispatcher` | `Metalama.Patterns.Caching.Backend/Implementation/ThreadPoolWorkItemDispatcher.cs` |
| `IClearableMemoryCache` | `Metalama.Patterns.Caching.Backend/Implementation/IClearableMemoryCache.cs` |
| `FakeCachingServices` | `tests/Metalama.Patterns.Caching.TestHelpers/FakeCachingServices.cs` |
