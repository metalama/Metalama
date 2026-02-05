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
