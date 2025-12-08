# RPC Tests Guide

## Key Patterns for RPC Tests

### Service Provider Setup

Use `AdditionalServiceCollection` to add services, then get the service provider from `testContext.ServiceProvider.Global.Underlying`:

```csharp
private static IAdditionalServiceCollection CreateAdditionalServices( TestSynchronizationProvider? syncProvider = null )
{
    var additionalServices = new AdditionalServiceCollection();
    additionalServices.AddUntypedGlobalService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );

    if ( syncProvider != null )
    {
        additionalServices.AddUntypedGlobalService( typeof(ITestSynchronizationProvider), syncProvider );
    }

    return additionalServices;
}

// In test:
using var testContext = this.CreateTestContext( CreateAdditionalServices( syncProvider ) );
var serviceProvider = testContext.ServiceProvider.Global.Underlying;
```

**Important:** The `AdditionalServiceCollection` must be passed to `CreateTestContext()`. Then use `testContext.ServiceProvider.Global.Underlying` to get the service provider with all registered services.

### Synchronization Points

For deterministic testing of race conditions, use `TestSynchronizationProvider`:

1. Create the sync provider before creating endpoints
2. Enable sync points BEFORE starting operations that will hit them
3. Pass the sync provider via `WithUntypedService`
4. Always call `ReleaseAll()` in cleanup to avoid deadlocks

```csharp
var syncProvider = new TestSynchronizationProvider();
var serviceProvider = testContext.ServiceProvider.Global.Underlying
    .WithUntypedService( typeof(ITestSynchronizationProvider), syncProvider );

try
{
    syncProvider.EnableSyncPoint( $"ServerEndpoint.AfterGetsClient:{pipeName}" );
    serverEndpoint.Start();

    await syncProvider.WaitForSyncPointReachedAsync( syncPointName, cancellationToken );
    // ... verify state ...
    syncProvider.ReleaseSyncPoint( syncPointName );
}
finally
{
    syncProvider.ReleaseAll();
}
```

Available sync points:
- `ServerEndpoint.AfterGetsClient:{pipeName}` - After server accepts client but before configuring RPC
- `ClientEndpoint.BeforeSignalingAwaiters:{pipeName}` - After client updates collections but before signaling awaiters

### Waiting for Server-Side Connection

The client's `ConnectAsync` returns when the client side connects, but the server may not have finished processing. Use the `ClientConnected` event to wait for server-side completion:

```csharp
var clientConnectedTcs = new TaskCompletionSource<bool>();
serverEndpoint.ClientConnected += () => clientConnectedTcs.TrySetResult( true );

serverEndpoint.Start();
await client.ConnectAsync( cancellationToken );
await clientConnectedTcs.Task.WithCancellation( cancellationToken );

// Now safe to check serverEndpoint.ClientCount
```

For multiple clients:
```csharp
var connectedCount = 0;
var allClientsConnectedTcs = new TaskCompletionSource<bool>();

serverEndpoint.ClientConnected += () =>
{
    if ( Interlocked.Increment( ref connectedCount ) == expectedCount )
    {
        allClientsConnectedTcs.TrySetResult( true );
    }
};
```

### Test Class Structure

Use partial classes to separate test types into individual files:

- `FooTests.cs` - Main test class with test methods
- `FooTests.TestServerEndpoint.cs` - Test server endpoint
- `FooTests.TestClientEndpoint.cs` - Test client endpoint
- `FooTests.ITestApi.cs` - API interface (must be `internal` not `private`)
- `FooTests.TestService.cs` - RPC service implementation
- `FooTests.TestClient.cs` - RPC client implementation

### API Interface Accessibility

When creating test RPC services, the API interface must be `internal` (not `private`) because it's used as a type parameter for `RpcService<T>`:

```csharp
// CORRECT - internal
internal interface ITestApi : IRpcApi { }

// WRONG - will cause CS0060 error
private interface ITestApi : IRpcApi { }
```

### RPC Client Events

`RpcClient` base class has an `EventReceived` event. Don't redeclare it in derived classes:

```csharp
// CORRECT - just inherit from RpcClient<T>
private sealed class TestClient : RpcClient<ITestApi>
{
    public TestClient( ClientEndpoint endpoint ) : base( endpoint ) { }
}

// Then subscribe in tests:
client.EventReceived += e => { /* handle event */ };
```

### Never Use Hardcoded Delays

Always use proper synchronization instead of `Task.Delay`:

```csharp
// WRONG
await Task.Delay( 100 );
Assert.Equal( 0, serverEndpoint.ClientCount );

// CORRECT - use TaskCompletionSource or events
var disconnectedTcs = new TaskCompletionSource<bool>();
// ... set up event to signal TCS ...
await disconnectedTcs.Task.WithCancellation( cancellationToken );
Assert.Equal( 0, serverEndpoint.ClientCount );
```

### Always Use Cancellation Tokens

Never await without a cancellation token:

```csharp
// WRONG
await someTask;

// CORRECT
await someTask.WithCancellation( testContext.CancellationToken );
```

## Test Categories

- **ServerEndpointTests** - Server-side connection handling, multiple clients, disconnections
- **ConnectCoreAsyncTests** - Client-side connection, duplicate handling, awaiter signaling
- **RpcServicesTests** - Multiple services, event dispatching, service lookup
- **RpcServiceRaiseEventTests** - Event broadcasting races, concurrent events
