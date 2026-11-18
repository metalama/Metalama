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

`MemoryCachingBackend` clears its own items one key at a time with `IMemoryCache.Remove`, so it reports the `Clear` feature on any `IMemoryCache`, and the other entries of a shared cache are kept. `IMemoryCache` declares no operation that compacts the cache, although `MemoryCache` has one. `IClearableMemoryCache` is an `IMemoryCache` that declares `Clear` and `Compact`. The backend calls `Compact` when a cache that it owns is cleared with `ClearCacheOptions.Compact`. It does not call `IClearableMemoryCache.Clear`.

### Concurrency in `MemoryCachingBackend`

`MemoryCachingBackend` keeps a backward dependency index: for each dependency key, the set of the keys of the items that declare it in their forward dependencies (`CacheItem.Dependencies`). The index is owned by the backend, in a `ConcurrentDictionary`, and is not stored in the `IMemoryCache`. The following rules keep the items and the index consistent when operations run concurrently.

- Every operation that changes the item of a key, or the registrations of that key in the index, holds the lock of that key. This applies to `SetItem`, `RemoveItem`, each step of an invalidation, each step of `Clear`, and the post-eviction callback. The lock of a key does not depend on the stored value, so two operations on the same key always exclude each other.
- A dependency set is locked only for one change or one copy. No thread acquires a key lock or another set lock while it holds a set lock, so the locks cannot form a cycle.
- A set that becomes empty is marked as removed and removed from the index as that exact instance. A thread that has read a removed set retries with the current set, so no key is registered in a set that no lookup reaches.
- The serializer and the size calculator run before any lock is acquired and before any state changes. An exception leaves the backend unchanged, and a size calculator or serializer that uses the backend cannot deadlock with it.
- Each stored entry carries a state object (`MemoryCacheItem.State`) that records whether the backend has already removed, replaced or evicted the entry. A post-eviction callback runs on the thread pool, possibly after a newer value has been stored under the same key. It does nothing for an entry that has already been handled, it unregisters only the dependencies that the newer value does not declare, and it raises `ItemRemoved` only when the key has no current value.
- After `SetItem` has called the `IMemoryCache`, it reads the stored entry and aligns the index with it. When the cache holds the new entry, the registrations of the previous value are replaced. When a size-limited cache has rejected the new entry, it has also removed the previous entry: the previous value is then handled as an evicted value, and `ItemRemoved` is raised for it. When the cache has thrown an exception and still holds the previous entry, the registrations of the new value are removed and the previous entry stays the current entry.
- An invalidation copies the dependency set, then processes each key under its lock. It removes a value only when the value still declares the invalidated dependency. It recurses into the dependents of every key, including a key whose item is absent, and it removes a key from the invalidated set only after its dependents have been processed. A concurrent invalidation of the same dependency therefore walks the same dependents before it returns. A set of visited keys stops the recursion on a cyclic dependency graph.
- When an item is removed or evicted while other items depend on its key, its dependencies stay registered. An invalidation of one of these dependencies still reaches the dependents. The registrations are released when no item that is in the cache depends on the key any more, directly or through the keys of other removed items. The removed items of a dependency cycle therefore do not keep the registrations of each other. The backend does not keep the dependencies of an item that is removed while no item depends on its key. An item that is stored later with a dependency on that key is therefore not reached through the dependencies of the removed item.
- `Clear` removes only the items of the current instance, one key at a time, except with `ClearCacheOptions.Compact` on a cache that the backend owns, where it compacts the cache.
- When `SetItem` replaces a value, it removes the items that depend on the key, directly and transitively, because they were computed from the replaced value. It does so after it has released the lock of the key, and it never removes the item that it has just stored, so an item that depends on its own key, directly or through a cycle, keeps its new value. The first value stored for a key removes nothing. A copy that `LayeredCachingBackendEnhancer` stores when it reads the remote layer is not a replacement and removes nothing either. The Redis backend has the same behavior for a replaced item.

The replacement value (tombstone) that `LayeredCachingBackendEnhancer` stores in its local layer when the remote layer is not blocking is stored even when the key is absent, with the priority `NeverRemove`, a size of zero, and an expiration converted to a duration with the clock of the backend. An invalidation also stores it for a dependent key whose item is absent, and keeps an existing replacement value.

### Read-through in `LayeredCachingBackendEnhancer`

A read that finds no item in the local layer reads the remote layer and copies the item into the local layer. The following rules prevent the copy of a value that a concurrent removal, invalidation or clearing has removed.

- A read is registered for its duration. A removal marks the pending reads of its key. An invalidation and a clearing mark every pending read, because an item can depend on the invalidated dependency through the keys of other items. A read that is marked after it has copied its item removes that copy, and only that copy.
- When the remote layer is blocking, a removal changes the local layer, then the remote layer, then marks the pending reads and changes the local layer again. A read that overlaps the change of the remote layer is either marked, or has copied its item before the second change of the local layer.
- A remote layer that reports that it is blocking must not return an item to a read that starts after the removal of that item has returned. A remote layer that reads from a replica does not satisfy this requirement, because a replica receives the removal asynchronously, and the removed value that it returns would stay in the local layer. The Redis backend of `Metalama.Premium` therefore sends its read commands to the master by default when it is created under a local layer (`RedisCachingBackendConfiguration.ReadCommandFlags`).
- When the remote layer is not blocking, the copy is conditional: the local layer does not replace a replacement value that is not older than the remote item. The decision is made under the lock of the key.
- A read requests the dependencies of an item from the remote layer only when its caller requests them, because a remote layer such as Redis needs additional round trips to read them. The local layer cannot register the dependencies of a copy that was read without them. It records the keys of these copies. When a dependency is invalidated through the layered backend, the local layer removes all of them, because any of them can depend on the invalidated dependency. It also removes the items that depend, directly or transitively, on the key of such a copy, for the same reason. The removed items are read again from the remote layer when they are needed. When the remote layer is not blocking, they are replaced with replacement values, so an item that does not depend on the invalidated dependency is reported as absent until it is stored again or until the replacement value expires.
- A read that finds a replacement value compares the timestamps also when its caller requests the dependencies. A replacement value has no dependencies, but it is not a copy whose dependencies are unknown.
- The other nodes do not remove their copies on an invalidation. They rely on the `ItemRemoved` event that the remote layer must raise for each item that it removes. A node also processes the `ItemRemoved` events of its own remote backend, because the remote layer can remove an item that the local layer keeps. The local layer does not know the dependencies of every copy. It also keeps the items that depend on a key when a new value is stored for that key and it holds no item for it, whereas the remote layer removes them if it holds a value for the key. For such an event, the local layer removes the item but keeps a replacement value, which can belong to a more recent removal. An item that was stored after the reported removal is removed too, and is read again from the remote layer.

The synchronization point of `LayeredCachingBackendEnhancer` is `ReadThrough:RemoteItemRead`, which a read reaches after it has read the remote item and before it copies it.

The synchronization points of `MemoryCachingBackend` are `RemoveItemImpl:ItemLocked`, `InvalidateDependencyImpl:DependencyLocked`, `InvalidateDependencyImpl:DependentsCopied`, `AddBackwardDependency:DependencySetRead`, `RemoveBackwardDependency:DependencySetRead` and `ClearCore:ClearRequested`. Their names are the constants of the nested class `MemoryCachingBackend.SyncPoints`. The event accessors of `CachingBackend` have the synchronization points of `CachingBackend.EventSyncPoints`. The other interleavings are forced in tests through a decorator of `IMemoryCache` (`InterceptingMemoryCache` in the unit tests), which blocks a thread at a chosen call of the cache.

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
