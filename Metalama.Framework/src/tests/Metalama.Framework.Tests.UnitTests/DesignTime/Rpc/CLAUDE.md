# RPC Tests Guide

## Architecture

All RPC tests inherit from `RpcUnitTestClass` and use `RpcTestContext` for test setup.

### Base Class: `RpcUnitTestClass`

```csharp
public abstract class RpcUnitTestClass : UnitTestClass
{
    private protected RpcTestContext CreateRpcTestContext();
}
```

### Test Context: `RpcTestContext`

`RpcTestContext` provides:
- `ServiceProvider` - The underlying `IServiceProvider` configured with RPC services
- `Global` - The `GlobalServiceProvider` for tests needing `WithService()` methods
- `SyncProvider` - For deterministic race condition testing
- `JsonSerializationBinderProvider` - For RPC serialization
- `CancellationToken` - For test timeout

On disposal, `RpcTestContext` automatically calls `SyncProvider.ReleaseAll()` to prevent deadlocks.

### Example Test

```csharp
public sealed class MyRpcTests : RpcUnitTestClass
{
    public MyRpcTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public async Task MyTest()
    {
        using var testContext = this.CreateRpcTestContext();

        var pipeName = $"{nameof(MyRpcTests)}_{Guid.NewGuid()}";

        using var serverEndpoint = new MyServerEndpoint( testContext.ServiceProvider, pipeName );
        serverEndpoint.Start();

        using var clientEndpoint = new MyClientEndpoint( testContext.ServiceProvider, pipeName );
        await clientEndpoint.ConnectAsync( testContext.CancellationToken );

        // ... test logic ...
    }
}
```

## Synchronization Points

For deterministic testing of race conditions, use `testContext.SyncProvider`:

```csharp
[Fact]
public async Task TestWithSyncPoint()
{
    using var testContext = this.CreateRpcTestContext();

    var pipeName = $"{nameof(MyRpcTests)}_{Guid.NewGuid()}";
    var syncPointName = $"ServerEndpoint.AfterGetsClient:{pipeName}";

    using var serverEndpoint = new MyServerEndpoint( testContext.ServiceProvider, pipeName );

    // Enable sync point BEFORE starting operations that hit it
    testContext.SyncProvider.EnableSyncPoint( syncPointName );

    serverEndpoint.Start();

    using var client = new MyClientEndpoint( testContext.ServiceProvider, pipeName );
    var connectTask = client.ConnectAsync( testContext.CancellationToken );

    // Wait for sync point
    await testContext.SyncProvider.WaitForSyncPointReachedAsync( syncPointName, testContext.CancellationToken );

    // Verify state at sync point...

    // Release sync point
    testContext.SyncProvider.ReleaseSyncPoint( syncPointName );

    await connectTask.WithCancellation( testContext.CancellationToken );
}
```

Available sync points:
- `ServerEndpoint.AfterGetsClient:{pipeName}` - After server accepts client but before configuring RPC
- `ClientEndpoint.BeforeSignalingAwaiters:{pipeName}` - After client updates collections but before signaling awaiters

## Waiting for Server-Side Connection/Disconnection

The client's `ConnectAsync` returns when the client side connects, but the server may not have finished processing. Use the `ClientConnected` and `ClientDisconnected` events:

```csharp
var clientConnectedTcs = new TaskCompletionSource<bool>();
serverEndpoint.ClientConnected += () => clientConnectedTcs.TrySetResult( true );

serverEndpoint.Start();
await client.ConnectAsync( testContext.CancellationToken );
await clientConnectedTcs.Task.WithCancellation( testContext.CancellationToken );

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

For waiting on client disconnections (e.g., before testing event broadcast to remaining clients):
```csharp
var clientDisconnectedTcs = new TaskCompletionSource<bool>();
serverEndpoint.ClientDisconnected += () => clientDisconnectedTcs.TrySetResult( true );

// Disconnect the client
client.Dispose();

// Wait for server to process the disconnection
await clientDisconnectedTcs.Task.WithCancellation( testContext.CancellationToken );

// Now safe to check serverEndpoint.ClientCount or broadcast events
```

## Test Class Structure

Use partial classes to separate test types into individual files:

- `FooTests.cs` - Main test class with test methods
- `FooTests.TestServerEndpoint.cs` - Test server endpoint
- `FooTests.TestClientEndpoint.cs` - Test client endpoint
- `FooTests.ITestApi.cs` - API interface (must be `internal` not `private`)
- `FooTests.TestService.cs` - RPC service implementation
- `FooTests.TestClient.cs` - RPC client implementation

### API Interface Accessibility

The API interface must be `internal` (not `private`) because it's used as a type parameter for `RpcService<T>`:

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

## Best Practices

### Never Use Hardcoded Delays

Always use proper synchronization instead of `Task.Delay`:

```csharp
// WRONG
await Task.Delay( 100 );
Assert.Equal( 0, serverEndpoint.ClientCount );

// CORRECT - use TaskCompletionSource or events
var disconnectedTcs = new TaskCompletionSource<bool>();
// ... set up event to signal TCS ...
await disconnectedTcs.Task.WithCancellation( testContext.CancellationToken );
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
