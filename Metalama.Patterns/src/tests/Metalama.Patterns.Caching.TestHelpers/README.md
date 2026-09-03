# Metalama.Patterns.Caching.TestHelpers

This package contains helper classes used, together with the `Metalama.Patterns.TestHelpers` package, when developing unit tests for a new `CachingBackend` implementation.

Unless you are developing your own `CachingBackend`, you don't need this package.

## Substituting the dependencies of a backend

A caching backend has three dependencies that this package can replace: the clock, the object that dispatches work items, and the memory cache. A test that substitutes them advances the clock and waits for the pending work items, instead of sleeping for a duration or waiting for a timeout.

| Type | Replaces | Product default |
|------|----------|-----------------|
| `FakeCachingServices` | The service provider given to the backend | None |
| `Microsoft.Extensions.Time.Testing.FakeTimeProvider` | `System.TimeProvider` | `TimeProvider.System` |
| `TestWorkItemDispatcher` | `Metalama.Patterns.Caching.Implementation.IWorkItemDispatcher` | `ThreadPoolWorkItemDispatcher` |
| `FakeMemoryCache` | `Microsoft.Extensions.Caching.Memory.IMemoryCache` | `Microsoft.Extensions.Caching.Memory.MemoryCache` |

`FakeCachingServices` creates the three implementations and registers them in a single service provider. Pass its `ServiceProvider` property to the backend under test. The backend then resolves the substitutes instead of the defaults.

### `FakeTimeProvider`

`FakeTimeProvider` comes from the `Microsoft.Extensions.TimeProvider.Testing` package. Its clock only moves when the test moves it. `FakeCachingServices.TimeProvider` exposes the instance, and the constructor of `FakeCachingServices` takes the instant at which the clock starts.

### `TestWorkItemDispatcher`

`TestWorkItemDispatcher` runs the work items on the thread pool, as the product implementation does, and counts the work items that have been queued and have not completed yet. `WhenPendingWorkItemsCompletedAsync` returns a task that completes when that count reaches zero.

The work items still run on real threads. The caching code blocks in several places, so a single pump thread executing the work that it queues to itself would deadlock. The class therefore offers a completion point, not an ordering guarantee.

A work item that queues another work item before it returns is covered. The count does not reach zero between the two, so the wait observes the whole chain.

### `FakeMemoryCache`

`FakeMemoryCache` reads a `TimeProvider` instead of the wall clock. An entry expires as soon as the clock passes its expiration instant. The class registers a timer with the `TimeProvider` for the earliest expiration instant, so advancing a `FakeTimeProvider` evicts the entries that fall due and invokes their post-eviction callbacks, without the test having to notify the cache.

`FakeMemoryCache` implements `IClearableMemoryCache`, so a backend that stores its entries in it reports the `Clear` feature, as it does with `MemoryCache`.

`FakeCachingServices` registers the memory cache as a single instance. Two backends that resolve it from the same service provider therefore share one store, which is not what the two layers of a layered backend need. Give the second layer its own store in that case.

## A worked example

The sequence is always the same: advance the clock, await the dispatcher, assert. `FakeCachingServices.AdvanceAsync` does the first two steps in one call.

```csharp
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;

public sealed class MemoryCachingBackendExpirationTests
{
    private static readonly DateTimeOffset _origin = new( 2026, 1, 1, 0, 0, 0, TimeSpan.Zero );

    [Fact]
    public async Task AbsoluteExpiration_RaisesItemRemoved_WhenTheClockAdvances()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var cancellationTokenSource = new CancellationTokenSource();

        using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration() ),
            fakes.ServiceProvider );

        backend.Initialize();

        CacheItemRemovedEventArgs? removedArgs = null;
        backend.ItemRemoved += ( _, args ) => removedArgs = args;

        const string key = "expiring-key";

        backend.SetItem(
            key,
            new CacheItem( "value", configuration: new CacheItemConfiguration { AbsoluteExpiration = TimeSpan.FromMinutes( 5 ) } ) );

        Assert.NotNull( backend.GetItem( key ) );

        await fakes.AdvanceAsync( TimeSpan.FromMinutes( 6 ), cancellationTokenSource.Token );

        Assert.Null( backend.GetItem( key ) );
        Assert.NotNull( removedArgs );
        Assert.Equal( CacheItemRemovedReason.Expired, removedArgs.RemovedReason );
    }
}
```

The call to `AdvanceAsync` returns when the whole chain has completed:

1. The clock passes the expiration instant of the entry.
2. `FakeMemoryCache` evicts the entry and invokes its post-eviction callback.
3. The backend queues a work item that raises `ItemRemoved`.
4. The count of pending work items of `TestWorkItemDispatcher` reaches zero.

There is no sleep and no timeout in the sequence.

Use `WhenPendingWorkItemsCompletedAsync` instead of `AdvanceAsync` when the operation under test is not the passage of time, for example an explicit `RemoveItem` whose event is raised on a work item.

## Substitution is opt-in

Substitution is opt-in per test class. A test class that does not create a `FakeCachingServices` keeps running against the real clock, the real thread pool and `MemoryCache`. The defaults of the backend are unchanged, so an existing test suite that ignores this package continues to behave as before.

Some test suites stay on the real thread pool on purpose, because they exist to exercise a real and contended one. In this repository, `AwaitableEventRaceTests` reproduces interleavings of `AwaitableEvent` between two real threads, `BackgroundTaskSchedulerEdgeCaseTests` exercises the concurrency limit and the overload detection of `BackgroundTaskScheduler`, and `AwaitableEventHangDiagnostic` is a load test that repeats the enqueue-then-await handshake a large number of times. A deterministic dispatcher would remove the very contention that these suites measure.

`BaseCacheBackendTests`, the shared base class of the backend test suites, stays on the real clock for the same kind of reason. The Redis and Azure backends of `Metalama.Premium` derive from it and run against a network and a real clock, which a fake clock cannot drive.
