// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Backstage.Extensibility;

/// <summary>
/// The implementation of <see cref="IEventDispatcher"/>. It owns one first-in, first-out queue and one consumer at a
/// time, so that events are delivered in the order of their publication.
/// </summary>
/// <remarks>
/// The consumer is a task that is started when an event is published to an idle queue and that exits when the queue
/// is empty. It is started without a cancellation token, because a task that is cancelled before it starts never runs
/// and would leave the queue in the running state forever.
/// </remarks>
internal sealed class EventDispatcher : IEventDispatcher, IDisposable
{
    private readonly ConcurrentQueue<object> _queue = new();
    private readonly ConcurrentDictionary<Type, ImmutableList<Subscription>> _subscriptions = new();
    private readonly object _consumerLock = new();
    private readonly ILogger _logger;
    private readonly IEventDispatcherObserver? _observer;

    /// <summary>
    /// The task that drains the queue, or <c>null</c> when the queue is idle.
    /// </summary>
    private Task? _consumer;

    private bool _isDisposed;

    public EventDispatcher( IServiceProvider serviceProvider )
    {
        this._logger = serviceProvider.GetRequiredBackstageService<EarlyLoggerFactory>().GetLogger( "Events" );
        this._observer = serviceProvider.GetBackstageService<IEventDispatcherObserver>();
    }

    /// <inheritdoc />
    public void Publish<TEvent>( TEvent @event )
        where TEvent : class
    {
        if ( @event == null )
        {
            throw new ArgumentNullException( nameof(@event) );
        }

        lock ( this._consumerLock )
        {
            if ( this._isDisposed )
            {
                this._logger.Trace?.Log( $"The event '{@event}' is dropped because the dispatcher is disposed." );

                return;
            }

            this._queue.Enqueue( @event );

            this._consumer ??= Task.Run( this.Consume );
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>( Action<TEvent> handler )
        where TEvent : class
    {
        var subscription = new Subscription( typeof(TEvent), e => handler( (TEvent) e ), this );

        this._subscriptions.AddOrUpdate(
            typeof(TEvent),
            _ => ImmutableList.Create( subscription ),
            ( _, list ) => list.Add( subscription ) );

        return subscription;
    }

    private void Unsubscribe( Subscription subscription )
    {
        while ( true )
        {
            if ( !this._subscriptions.TryGetValue( subscription.EventType, out var list ) )
            {
                return;
            }

            var newList = list.Remove( subscription );

            if ( this._subscriptions.TryUpdate( subscription.EventType, newList, list ) )
            {
                return;
            }
        }
    }

    /// <inheritdoc />
    public Task CompleteAsync( CancellationToken cancellationToken )
    {
        Task? consumer;

        lock ( this._consumerLock )
        {
            consumer = this._consumer;
        }

        if ( consumer == null )
        {
            return Task.CompletedTask;
        }

        return WaitForConsumerAsync( consumer, cancellationToken );
    }

    private static async Task WaitForConsumerAsync( Task consumer, CancellationToken cancellationToken )
    {
        // The consumer never faults, because every handler runs inside a try block, so waiting for it cannot throw
        // anything but the cancellation.
        var cancellation = new TaskCompletionSource<bool>();

        using ( cancellationToken.Register( () => cancellation.TrySetCanceled( cancellationToken ) ) )
        {
            await Task.WhenAny( consumer, cancellation.Task );
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private void Consume()
    {
        while ( true )
        {
            object? @event;

            lock ( this._consumerLock )
            {
                if ( !this._queue.TryDequeue( out @event ) )
                {
                    // The queue is empty: the consumer exits, and the next publication starts a new one. The decision
                    // is taken under the lock, so a publication cannot slip in between the check and the exit.
                    this._consumer = null;

                    return;
                }
            }

            this.Deliver( @event );
        }
    }

    private void Deliver( object @event )
    {
        if ( !this._subscriptions.TryGetValue( @event.GetType(), out var subscriptions ) || subscriptions.IsEmpty )
        {
            this._logger.Warning?.Log( $"The event '{@event}' was published but nothing has subscribed to '{@event.GetType().Name}'." );
            this._observer?.OnEventUnhandled( @event );

            return;
        }

        foreach ( var subscription in subscriptions )
        {
            try
            {
                subscription.Handler( @event );
            }
            catch ( Exception e )
            {
                this._logger.LogException( e, $"A subscriber of '{@event.GetType().Name}' failed" );
                this._observer?.OnSubscriberFailed( @event, e );
            }
        }
    }

    /// <summary>
    /// Stops accepting events. The events already enqueued are still delivered by the running consumer.
    /// </summary>
    public void Dispose()
    {
        lock ( this._consumerLock )
        {
            this._isDisposed = true;
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly EventDispatcher _dispatcher;

        public Subscription( Type eventType, Action<object> handler, EventDispatcher dispatcher )
        {
            this.EventType = eventType;
            this.Handler = handler;
            this._dispatcher = dispatcher;
        }

        public Type EventType { get; }

        public Action<object> Handler { get; }

        public void Dispose() => this._dispatcher.Unsubscribe( this );
    }
}
