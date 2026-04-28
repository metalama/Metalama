# RPC Infrastructure Guide

## Overview

This project provides bidirectional RPC (Remote Procedure Call) communication using named pipes and StreamJsonRpc. It enables design-time services to communicate between different processes (e.g., Visual Studio host process and analyzer process).

## Architecture

### Communication Model

```
┌─────────────────────┐     Named Pipe      ┌─────────────────────┐
│   ServerEndpoint    │◄───────────────────►│   ClientEndpoint    │
│   (VS Host)         │                     │   (Analyzer)        │
├─────────────────────┤                     ├─────────────────────┤
│   RpcService<T>     │ ◄── API calls ────  │   RpcClient<T>      │
│                     │ ── Events ────────► │                     │
└─────────────────────┘                     └─────────────────────┘
```

### Class Hierarchy

```
BaseEndpoint (abstract)
├── ServerEndpoint (abstract)
│   └── Concrete servers (RpcServiceProviderServerEndpoint, etc.)
└── ClientEndpoint (abstract)
    └── Concrete clients (CodeLensProcessClientEndpoint, etc.)

RpcService (abstract)
└── RpcService<TApi> (generic, with callback support)
    └── Concrete services

RpcClient (abstract)
└── RpcClient<TApi> (generic, with remote API proxy)
    └── Concrete clients
```

## Key Files

| File | Purpose |
|------|---------|
| `BaseEndpoint.cs` | Base class with pipe name, logging, background tasks, dispose pattern |
| `ServerEndpoint.cs` | Server-side: accepts connections, manages services, raises events |
| `ClientEndpoint.cs` | Client-side: connects to server, creates client proxies, receives events |
| `RpcService.cs` | Server-side service base class with event broadcasting |
| `RpcClient.cs` | Client-side proxy base class with remote API access |

## Critical Patterns

### 1. Background Task Lifecycle

`ExecuteBackgroundTask` schedules fire-and-forget tasks with exception handling:

```csharp
// In BaseEndpoint.cs:129
protected void ExecuteBackgroundTask( Func<CancellationToken, Task> action, string description, bool registerTask = true )
```

**Important**: `WhenBackgroundTasksCompletedAsync` loops until all tasks complete, including tasks scheduled during completion of other tasks:

```csharp
// In BaseEndpoint.cs:202
public async Task WhenBackgroundTasksCompletedAsync( CancellationToken cancellationToken )
{
    while ( true )
    {
        var tasks = this._backgroundTasks.Values.ToArray();
        if ( tasks.Length == 0 ) return;
        await Task.WhenAll( tasks.Select( t => t.Task ) )...
    }
}
```

### 2. Connection Lifecycle

**First connection**: Full setup with callback registration
- Client creates `Callback` instance implementing `IRpcCallback`
- Server uses callback to send events to client

**Subsequent connections** (to additional service pipes):
- Client uses `NullCallback` (no-op) to avoid duplicate event delivery
- Events only sent via first connection

### 3. Event Broadcasting

Two patterns for raising events from services:

```csharp
// Async - awaits all client notifications
await RaiseEventAsync( new MyEventData(...), cancellationToken );

// Fire-and-forget - schedules as background task
RaiseEvent( new MyEventData(...) );
```

Events flow: `RpcService.RaiseEventAsync` → `IRpcCallback.RaiseEventAsync` → `ClientEndpoint.OnEventReceivedAsync` → `RpcClient.EventReceived`

### 4. Service Registration

**Static services** (at startup):
```csharp
using var server = new MyServerEndpoint( serviceProvider, pipeName, [serviceFactory1, serviceFactory2] );
server.Start();
```

**Dynamic services** (after startup):
```csharp
server.AddServices( [newServiceFactory] );
// Creates additional pipe: {pipeName}_{serviceIndex}
```

### 5. Serialization Strategy

`JsonSerializationBinder` handles multi-version assembly support:
- Metalama assemblies: full assembly name (version-specific)
- Non-Metalama assemblies: simple name (version-agnostic)

This allows different Metalama versions to be loaded in the same AppDomain while maintaining serialization compatibility.

## Test Synchronization

For deterministic testing, use `ITestSynchronizationProvider`:

```csharp
// Enable sync point BEFORE the operation that hits it
testContext.SyncProvider.EnableSyncPoint( "ClassName.SyncPointName:Context" );

// Perform operation that will block at sync point
server.AddServices( [...] );

// Wait for sync point to be reached
await testContext.SyncProvider.WaitForSyncPointReachedAsync( "ClassName.SyncPointName:Context", ct );

// Optionally modify state while paused

// Release to continue
testContext.SyncProvider.ReleaseSyncPoint( "ClassName.SyncPointName:Context" );
```

**Naming convention**: `ClassName.Location:Context` (e.g., `RpcServiceProviderClientEndpoint.BeforeExtensionCheck:TheExtension`)

## Common Tasks

### Creating a New RPC Service

1. Define API interface extending `IRpcApi`:
```csharp
public interface IMyServiceApi : IRpcApi
{
    Task<string> DoSomethingAsync( string input );
}
```

2. Implement server-side service:
```csharp
public sealed class MyService : RpcService<IMyServiceApi>
{
    public override IMyServiceApi CreateApi( IRpcEventSender eventSender )
        => new Api( this, eventSender );

    private sealed class Api : IMyServiceApi
    {
        public Task<string> DoSomethingAsync( string input ) => Task.FromResult( input.ToUpper() );
    }
}
```

3. Implement client-side proxy:
```csharp
public sealed class MyClient : RpcClient<IMyServiceApi>
{
    public MyClient( ClientEndpoint endpoint ) : base( endpoint ) { }
}
```

### Waiting for Background Tasks in Tests

Always wait for both server and client tasks:
```csharp
await serverEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );
await clientEndpoint.WhenBackgroundTasksCompletedAsync( testContext.CancellationToken );
```

### Handling Client Disconnection

Use `ClientConnected` and `ClientDisconnected` events on `ServerEndpoint`:
```csharp
var disconnectedTcs = new TaskCompletionSource<bool>();
serverEndpoint.ClientDisconnected += () => disconnectedTcs.TrySetResult( true );

client.Dispose();
await disconnectedTcs.Task.WithCancellation( cancellationToken );
// Now safe to check state after disconnection
```

## Notifications Subsystem

Located in `Notifications/` folder:

| File | Purpose |
|------|---------|
| `IEventHubRpcApi.cs` | Event hub subscription API |
| `EventHubRpcService.cs` | Broadcasts events to subscribed clients |
| `CompilationResultChangedEventData.cs` | Event when compilation result changes |
| `EndpointChangedEventData.cs` | Event when endpoint configuration changes |
| `CodeLensProcessClientEndpoint.cs` | CodeLens integration client |

## Debugging Tips

1. **Connection issues**: Check `PipeName` matches on both endpoints
2. **Missing events**: Verify client subscribed before event raised
3. **Serialization errors**: Check `JsonSerializationBinder` includes required assemblies
4. **Race conditions**: Use sync points in tests, never hardcoded delays
5. **Task completion**: Use `WhenBackgroundTasksCompletedAsync` in tests

## Related Documentation

- **Scope and version-compatibility rules**: `Metalama.Framework/docs/cross-process-communication.md`. **Read this before extending the Rpc layer or before considering exposing it to a non-Metalama consumer.** This layer is *same-version, cross-process only*; cross-version surfaces belong in `Metalama.Framework.DesignTime.Contracts`.
- Test patterns: `tests/Metalama.Framework.Tests.UnitTests/DesignTime/Rpc/CLAUDE.md`
