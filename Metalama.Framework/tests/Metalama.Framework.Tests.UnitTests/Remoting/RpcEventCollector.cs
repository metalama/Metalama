// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.Remoting;

internal sealed class RpcEventCollector
{
    private readonly ConcurrentQueue<RpcEventData> _events = new();
    private readonly ConcurrentDictionary<Awaiter, Awaiter> _awaiters = new();

    public IReadOnlyCollection<RpcEventData> Events => this._events;

    public void OnEventReceived( RpcEventData eventData )
    {
        this._events.Enqueue( eventData );

        foreach ( var awaiter in this._awaiters.Values.ToArray() )
        {
            if ( awaiter.Condition( this ) )
            {
                awaiter.TaskCompletionSource.SetResult( true );
                this._awaiters.TryRemove( awaiter, out _ );
            }
        }
    }

    public Task WhenTrueAsync( Func<RpcEventCollector, bool> condition, CancellationToken cancellationToken )
    {
        if ( condition( this ) )
        {
            return Task.CompletedTask;
        }
        else
        {
            var awaiter = new Awaiter( condition );
            this._awaiters.TryAdd( awaiter, awaiter );

            if ( condition( this ) )
            {
                return Task.CompletedTask;
            }
            else
            {
#pragma warning disable VSTHRD003
                return awaiter.TaskCompletionSource.Task.WithCancellation( cancellationToken );
#pragma warning restore VSTHRD003
            }
        }
    }

    private sealed class Awaiter
    {
        public Func<RpcEventCollector, bool> Condition { get; }

        public TaskCompletionSource<bool> TaskCompletionSource { get; } = new();

        public Awaiter( Func<RpcEventCollector, bool> condition )
        {
            this.Condition = condition;
        }
    }
}