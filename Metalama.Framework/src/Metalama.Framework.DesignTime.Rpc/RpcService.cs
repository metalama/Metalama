// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;
using System.Collections.Concurrent;

// ReSharper disable InconsistentlySynchronizedField

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Base class for RPC services implementations, i.e. on the server side.
/// </summary>
public abstract class RpcService
{
    private static int _nextId;
    private readonly TaskCompletionSource<bool> _initialized = new();
    private readonly int _id = Interlocked.Increment( ref _nextId );

    protected ILogger Logger { get; }

    protected RpcService( ServerEndpoint serverEndpoint )
    {
        this.Logger = serverEndpoint.LoggerFactory.GetLogger( this.GetType().Name ).WithPrefix( this._id.ToString() );
        this.Logger.Trace?.Log( $"Instantiating." );
    }

    protected Task WaitUntilInitializedAsync( CancellationToken cancellationToken = default )
        => this._initialized.Task.WarnIfLongAsync( this.Logger, nameof(this.WaitUntilInitializedAsync), cancellationToken );

    internal abstract void ConfigureRpc( JsonRpc rpc );

    /// <summary>
    /// Called by the server endpoint after <see cref="JsonRpc.StartListening"/> has been called on the rpc.
    /// Subclasses register the rpc with their callback bookkeeping at this point so that event broadcasting
    /// (such as <c>RaiseEventAsync</c>) never targets a rpc whose <c>StartListening</c> has not yet been
    /// called, which would throw <c>"This operation is not allowed before listening for messages has started."</c>
    /// </summary>
    internal virtual void OnRpcStarted( JsonRpc rpc ) { }

    protected internal void OnRpcConnected()
    {
        this.Logger.Trace?.Log( nameof(this.OnRpcConnected) );
        this._initialized.TrySetResult( true );
    }

    internal virtual void OnRpcDisconnected( JsonRpc rpc )
    {
        this.Logger.Trace?.Log( nameof(this.OnRpcDisconnected) );
    }

    /// <summary>
    /// Helper for broadcasting an event to one client. Swallows any per-client failure so a single
    /// transitioning client cannot fail the broadcast for the others. Used by both
    /// <see cref="RpcService{TApi}.RaiseEventAsync"/> (which iterates <c>_clients</c>) and
    /// <c>EventHubRpcService.ForwardEventAsync</c> (which iterates <see cref="RpcService{TApi}.Apis"/>).
    /// The per-rpc lock in <see cref="RpcService{TApi}.OnRpcDisconnected"/> only narrows the race
    /// window — it cannot eliminate it because broadcast iteration is unlocked by design (taking
    /// a lock at broadcast time would block disconnect cleanup behind a network call).
    /// </summary>
    protected async Task SafeBroadcastInvokeAsync( Func<CancellationToken, Task> invoke, CancellationToken cancellationToken )
    {
        try
        {
            await invoke( cancellationToken );
        }
        catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested )
        {
            throw;
        }
        catch ( Exception ex )
        {
            this.Logger.Warning?.Log( $"Broadcast: per-client failure; skipping to keep broadcast intact. {ex}" );
        }
    }

    public override string ToString() => $"{{{this.GetType().Name}, Id={this._id}}}";
}

/// <summary>
/// Base class for an RPC service that has a callback.
/// </summary>
public abstract class RpcService<TApi> : RpcService, IDisposable where TApi : IRpcApi
{
    private readonly ConcurrentDictionary<JsonRpc, IRpcCallback> _clients = new();
    private readonly ConcurrentDictionary<JsonRpc, TApi> _apis = new();

    // Per-rpc registration entries. Created in ConfigureRpc, removed in OnRpcDisconnected. Each entry
    // owns its own lock so that OnRpcStarted (promotion to _clients/_apis) and OnRpcDisconnected (cleanup)
    // are serialized for the same rpc only. Operations on different rpcs do not contend, so a disconnect
    // on one client cannot be blocked behind another client's ongoing promotion.
    private readonly ConcurrentDictionary<JsonRpc, RegistrationEntry> _registrations = new();

    protected RpcService( ServerEndpoint serverEndpoint ) : base( serverEndpoint ) { }

    public virtual void Dispose() { }

    internal override void ConfigureRpc( JsonRpc rpc )
    {
        var client = rpc.Attach<IRpcCallback>();
        var implementation = this.CreateApi( new EventSender( client ) );
        rpc.AddLocalRpcTarget( implementation, null );
        this._registrations.TryAdd( rpc, new RegistrationEntry( client, implementation ) );
    }

    internal override void OnRpcStarted( JsonRpc rpc )
    {
        if ( !this._registrations.TryGetValue( rpc, out var entry ) )
        {
            // OnRpcDisconnected already removed the entry — nothing to promote.
            return;
        }

        lock ( entry.Lock )
        {
            this.OnPendingClientPromoting();

            if ( entry.IsDisconnected )
            {
                // OnRpcDisconnected ran and removed the entry from _registrations. The promotion has
                // nothing to do: leaving _clients/_apis untouched keeps them free of the dead rpc.
                return;
            }

            this._clients.TryAdd( rpc, entry.Callback );
            this._apis.TryAdd( rpc, entry.Api );
        }
    }

    /// <summary>
    /// Test extension point invoked inside <see cref="OnRpcStarted"/> while holding the per-rpc
    /// registration lock, before promotion to the live <c>_clients</c>/<c>_apis</c> dictionaries.
    /// Production code does nothing; tests override this to deterministically inject a concurrent
    /// disconnect during the promotion window.
    /// </summary>
    protected virtual void OnPendingClientPromoting() { }

    /// <summary>
    /// Test extension point invoked at the very entry of <see cref="OnRpcDisconnected"/>, before
    /// the per-rpc registration lock is acquired. Production code does nothing; tests use it to
    /// observe that a disconnect is being processed even when its per-rpc lock is currently held
    /// by a paused <see cref="OnRpcStarted"/>.
    /// </summary>
    protected virtual void OnRpcDisconnecting() { }

    internal override void OnRpcDisconnected( JsonRpc rpc )
    {
        this.OnRpcDisconnecting();

        if ( !this._registrations.TryRemove( rpc, out var entry ) )
        {
            // Either ConfigureRpc never ran for this rpc on this service, or OnRpcDisconnected ran
            // twice (StreamJsonRpc raises Disconnected at most once, but be defensive).
            return;
        }

        // Mark IsDisconnected BEFORE acquiring entry.Lock. A concurrent OnRpcStarted that is past
        // its _registrations lookup but still waiting on the lock will observe this flag (volatile
        // read inside the lock) when it resumes and skip the _clients/_apis TryAdd. Setting the
        // flag outside the lock means we never need to actually hold the lock for correctness —
        // the in-lock TryRemove below is only an optimization for the case where the entry was
        // already promoted.
        entry.IsDisconnected = true;

        // Use TryEnter, not Monitor.Enter: OnRpcDisconnected can run on a disposal/cancellation
        // thread that is itself blocking the lock holder. Example seen in CI: a test cancellation
        // token fires, the CTS.Cancel cascade runs async continuations inline on the timer thread,
        // disposal of the endpoint triggers JsonRpc.Disconnected synchronously, and OnRpcDisconnected
        // ends up needing entry.Lock — which is held by an OnRpcStarted whose user callback is
        // synchronously waiting for a signal that the very same thread is supposed to deliver after
        // disposal returns. A blocking Enter deadlocks; TryEnter degrades to best-effort cleanup
        // and lets the cancellation cascade complete.
        var lockTaken = false;

        try
        {
            Monitor.TryEnter( entry.Lock, TimeSpan.FromSeconds( 5 ), ref lockTaken );

            // _apis must be cleaned up alongside _clients so that subscribers iterating Apis
            // (e.g. EventHubRpcService.ForwardEventAsync) do not invoke a stored IRpcEventSender
            // over a dead JsonRpc. The IsDisconnected flag set above prevents a concurrent
            // OnRpcStarted from re-adding to either dictionary, so removing outside the lock
            // (lockTaken == false) is also safe: any TryAdd that already happened is undone here,
            // and any TryAdd still pending is skipped by the IsDisconnected check.
            this._clients.TryRemove( rpc, out _ );
            this._apis.TryRemove( rpc, out _ );

            if ( !lockTaken )
            {
                this.Logger.Warning?.Log(
                    $"OnRpcDisconnected: could not acquire registration lock for rpc within timeout; cleanup proceeded without the lock." );
            }
        }
        finally
        {
            if ( lockTaken )
            {
                Monitor.Exit( entry.Lock );
            }
        }
    }

    protected IEnumerable<TApi> Apis => this._apis.Values;

    protected abstract TApi CreateApi( IRpcEventSender eventSender );

    protected async Task RaiseEventAsync( RpcEventData eventData, CancellationToken cancellationToken )
    {
        await this.WaitUntilInitializedAsync( cancellationToken );

        if ( this._clients.Count == 0 )
        {
            this.Logger.Trace?.Log( $"RaiseEventAsync: No clients, nothing to do for event {eventData.Category}." );

            return;
        }

        this.Logger.Trace?.Log( $"RaiseEventAsync: Notifying {this._clients.Count} clients with event {eventData.Category}." );

        var envelope = new RpcEventEnvelope( typeof(TApi).Name, eventData );

        await Task.WhenAll( this._clients.Values.Select( c => this.SafeBroadcastInvokeAsync( ct => c.RaiseEventAsync( envelope, ct ), cancellationToken ) ) )
            .WithCancellation( cancellationToken );
    }

    protected void RaiseEvent( RpcEventData eventData, CancellationToken cancellationToken = default )
    {
        // TODO: Exception handling.
        _ = this.RaiseEventAsync( eventData, cancellationToken );
    }

    private sealed class EventSender : IRpcEventSender
    {
        private readonly IRpcCallback _client;

        public EventSender( IRpcCallback client )
        {
            this._client = client;
        }

        public Task RaiseEventAsync( RpcEventData eventData, CancellationToken cancellationToken )
            => this._client.RaiseEventAsync( new RpcEventEnvelope( typeof(TApi).Name, eventData ), cancellationToken );
    }

    private sealed class RegistrationEntry
    {
        // Volatile because OnRpcDisconnected writes it outside entry.Lock to avoid blocking on the
        // lock (see OnRpcDisconnected comments). OnRpcStarted reads it inside the lock, but the
        // lock acquire only fences the reader's own operations — without volatile, the unlocked
        // write may not be visible.
        private volatile bool _isDisconnected;

        public RegistrationEntry( IRpcCallback callback, TApi api )
        {
            this.Callback = callback;
            this.Api = api;
        }

        public IRpcCallback Callback { get; }

        public TApi Api { get; }

        public object Lock { get; } = new();

        public bool IsDisconnected
        {
            get => this._isDisconnected;
            set => this._isDisconnected = value;
        }
    }
}