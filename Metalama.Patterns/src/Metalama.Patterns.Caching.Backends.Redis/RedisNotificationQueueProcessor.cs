// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using static Flashtrace.Messages.FormattedMessageBuilder;

namespace Metalama.Patterns.Caching.Backends.Redis;

internal sealed class RedisNotificationQueueProcessor : IDisposable
{
    private const int _connectDelay = 10;

    private readonly FlashtraceSource _logger;
    private readonly BlockingCollection<RedisNotification?> _notificationQueue = new();
    private readonly ManualResetEvent _queueProcessingAllowedEvent = new( true );
    private readonly TaskCompletionSource<bool> _disposeTask = new();

    private readonly Thread _notificationProcessingThread;
    private readonly TaskCompletionSource<bool> _notificationProcessingThreadCompleted = new();
    private readonly ImmutableArray<RedisChannel> _channels;
    private readonly Action<RedisNotification> _handler;
    private readonly TimeSpan _connectionTimeout;
    private readonly StackTrace _allocationStackTrace = new();

    private readonly string _id;
    private readonly IRedisBackendObserver? _observer;

    private volatile TaskCompletionSource<bool> _queueEmptyTask = new();
    private volatile int _status;

    private RedisNotificationQueueProcessor(
        string id,
        IConnectionMultiplexer connection,
        ImmutableArray<RedisChannel> channels,
        Action<RedisNotification> handler,
        TimeSpan connectionTimeout,
        IServiceProvider? serviceProvider )
    {
        this._id = id;
        this._channels = channels;
        this._handler = handler;
        this._connectionTimeout = connectionTimeout;
        this.Subscriber = connection.GetSubscriber();
        this._logger = serviceProvider.GetFlashtraceSource( this.GetType(), FlashtraceRole.Caching );
        this._observer = (IRedisBackendObserver?) serviceProvider?.GetService( typeof(IRedisBackendObserver) );

        this._notificationProcessingThread = new Thread( ProcessNotificationQueue )
        {
            Name = nameof(DependenciesRedisCachingBackend) + ".ProcessNotificationQueue"
        };
    }

    public ISubscriber Subscriber { get; }

    ~RedisNotificationQueueProcessor()
    {
        try
        {
            this._logger.Error.Write(
                Formatted(
                    "The resource {ObjectId} has not been properly disposed. Allocation stack trace:\n{AllocStackTrace}",
                    this._id,
                    this._allocationStackTrace.ToString() ) );

            try
            {
                this.Dispose( false );
            }
            catch ( Exception e )
            {
                this._logger.Error.Write( Formatted( "Exception when finalizing the RedisNotificationQueue." ), e );
                this.BackgroundTaskExceptions++;
            }
        }
        catch
        {
            // ignored
        }
    }

    private void Init()
    {
        var startedAt = DateTime.Now;

        foreach ( var channel in this._channels )
        {
            this.Subscriber.Subscribe( channel, this.OnNotificationReceived );

            while ( !this.Subscriber.IsConnected( channel ) )
            {
                this._logger.Debug.IfEnabled?.Write( Formatted( "Not connected to {Channel}. Waiting {Delay} ms and retrying.", channel, _connectDelay ) );

                Thread.Sleep( _connectDelay );

                if ( DateTime.Now > startedAt + this._connectionTimeout )
                {
                    throw new TimeoutException( "Could not connect to a Redis server in the connection timeout." );
                }
            }
        }

        this.Subscriber.Ping();

        this._notificationProcessingThread.Start( new WeakReference<RedisNotificationQueueProcessor>( this ) );
    }

    private async Task<RedisNotificationQueueProcessor> InitAsync( CancellationToken cancellationToken )
    {
        foreach ( var channel in this._channels )
        {
            cancellationToken.ThrowIfCancellationRequested();

            await this.Subscriber.SubscribeAsync( channel, this.OnNotificationReceived );

            while ( !this.Subscriber.IsConnected( channel ) )
            {
                cancellationToken.ThrowIfCancellationRequested();

                this._logger.Debug.IfEnabled?.Write( Formatted( "Not connected to {Channel}. Waiting {Delay} ms and retrying.", channel, _connectDelay ) );

                await Task.Delay( _connectDelay, cancellationToken );
            }
        }

        await this.Subscriber.PingAsync();

        this._notificationProcessingThread.Start( new WeakReference<RedisNotificationQueueProcessor>( this ) );

        return this;
    }

    public static RedisNotificationQueueProcessor Create(
        string id,
        IConnectionMultiplexer connection,
        ImmutableArray<RedisChannel> channels,
        Action<RedisNotification> handler,
        TimeSpan connectionTimeout,
        IServiceProvider? serviceProvider )
    {
        var queue = new RedisNotificationQueueProcessor( id, connection, channels, handler, connectionTimeout, serviceProvider );
        queue.Init();

        return queue;
    }

    public static Task<RedisNotificationQueueProcessor> CreateAsync(
        string id,
        IConnectionMultiplexer connection,
        ImmutableArray<RedisChannel> channels,
        Action<RedisNotification> handler,
        TimeSpan connectionTimeout,
        IServiceProvider? serviceProvider,
        CancellationToken cancellationToken )
    {
        var queue = new RedisNotificationQueueProcessor( id, connection, channels, handler, connectionTimeout, serviceProvider );

        return queue.InitAsync( cancellationToken );
    }

    private void OnNotificationReceived( RedisChannel channel, RedisValue value )
    {
        this._logger.Debug.IfEnabled?.Write( Formatted( "Received notification '{Value}' on channel '{Channel}'.", value, channel ) );

        if ( !this._notificationQueue.IsAddingCompleted )
        {
            try
            {
                this._notificationQueue.Add( new RedisNotification { Channel = channel, Value = value } );

                return;
            }
            catch ( ObjectDisposedException ) { }
        }

        this._logger.Debug.IfEnabled?.Write( Formatted( "The notification was not queued because the queue was already disposed." ) );
    }

    private static void ProcessNotificationQueue( object? state )
    {
        if ( state is not WeakReference<RedisNotificationQueueProcessor> queueRef )
        {
            throw new CachingAssertionFailedException( "Did not get a WeakReference<RedisNotificationQueue>." );
        }

        if ( !queueRef.TryGetTarget( out var queue ) )
        {
            return;
        }

        queue._observer?.OnNotificationThreadStarted();
        var logger = queue._logger;
        var id = queue._id;
        var blockingCollection = queue._notificationQueue;

        try
        {
            logger.Debug.IfEnabled?.Write( Formatted( "The {ThreadName} thread for object {ObjectId} has started.", nameof(ProcessNotificationQueue), id ) );

            while ( true )
            {
                logger.Debug.IfEnabled?.Write( Formatted( "Waiting for a notification." ) );
                var notification = blockingCollection.Take();
                logger.Debug.IfEnabled?.Write( Formatted( "Got a notification." ) );

                if ( notification == null )
                {
                    logger.Debug.IfEnabled?.Write( Formatted( "Received a null notification: exiting." ) );

                    return;
                }

                if ( !queueRef.TryGetTarget( out queue ) )
                {
                    logger.Debug.IfEnabled?.Write( Formatted( "Cannot dereference the queue: exiting." ) );

                    return;
                }

                if ( queue._status == (int) Status.Suspended )
                {
                    logger.Warning.IfEnabled?.Write( Formatted( "Notification processing is suspended." ) );

                    queue._queueProcessingAllowedEvent.WaitOne();

                    logger.Warning.IfEnabled?.Write( Formatted( "Notification processing is resumed." ) );
                }

                try
                {
                    using ( var activity =
                           logger.Default.OpenActivity(
                               Formatted(
                                   "Processing notification '{Value}' received on channel '{Channel}'",
                                   notification.Value.Value,
                                   notification.Value.Channel ) ) )
                    {
                        try
                        {
                            queue._handler( notification.Value );

                            activity.SetSuccess();
                        }
                        catch ( Exception e )
                        {
                            logger.Error.Write( Formatted( $"Got an exception {e.Message}" ) );

                            queue.BackgroundTaskExceptions++;

                            activity.SetException( e );
                        }
                    }

                    if ( blockingCollection.Count == 0 )
                    {
                        // Signaling an event whenever the queue is empty.
                        Interlocked.Exchange( ref queue._queueEmptyTask, new TaskCompletionSource<bool>() ).SetResult( true );
                    }
                }
                catch ( Exception e )
                {
                    // If we have any unhandled exception here, don't want to crash the process, but instead
                    // we want to continue processing other queue items.
                    logger.Error.Write( Formatted( "Exception while processing the notification '{Notification}'.", notification ), e );
                    queue.BackgroundTaskExceptions++;
                }
            }
        }
        finally
        {
            if ( queueRef.TryGetTarget( out queue ) )
            {
                queue._notificationProcessingThreadCompleted.TrySetResult( true );
                queue._observer?.OnNotificationThreadCompleted();
            }

            logger.Debug.IfEnabled?.Write( Formatted( "The {ThreadName} thread for object {ObjectId} has completed.", nameof(ProcessNotificationQueue), id ) );
        }
    }

    internal void SuspendProcessing()
    {
        if ( !this.TryChangeStatus( Status.Default, Status.Suspended ) )
        {
            throw new InvalidOperationException();
        }

        this._queueProcessingAllowedEvent.Reset();
    }

    internal void ResumeProcessing()
    {
        if ( !this.TryChangeStatus( Status.Suspended, Status.Default ) )
        {
            throw new InvalidOperationException();
        }

        this._queueProcessingAllowedEvent.Set();
    }

    internal Task WhenQueueEmpty()
    {
        // This is probably not thread safe, but this should be good enough for unit testing.
        Thread.MemoryBarrier();

        if ( this._status >= (int) Status.Disposing || this._notificationQueue.Count == 0 )
        {
            return Task.FromResult( true );
        }
        else
        {
            return this._queueEmptyTask.Task;
        }
    }

    private bool TryChangeStatus( Status expectedStatus, Status newStatus )
    {
        return Interlocked.CompareExchange( ref this._status, (int) newStatus, (int) expectedStatus ) == (int) expectedStatus;
    }

    private void ChangeStatus( Status newStatus )
    {
        this._status = (int) newStatus;
        Thread.MemoryBarrier();
    }

    public void Dispose()
    {
        this.Dispose( true );
    }

    private void Dispose( bool disposing )
    {
        using ( var activity = this._logger.Default.OpenActivity( Formatted( "Dispose( queue: {Queue} )", this._id ) ) )
        {
            try
            {
                if ( !this.TryChangeStatus( Status.Default, Status.Disposing ) )
                {
                    activity.SetOutcome( FlashtraceLevel.Debug, Formatted( "The method was already called." ) );
                    this._disposeTask.Task.Wait();

                    return;
                }

                try
                {
                    if ( disposing )
                    {
                        if ( this.Subscriber.Multiplexer.IsConnected )
                        {
                            try
                            {
                                this._logger.Trace.IfEnabled?.Write( Formatted( "Unsubscribing." ) );
                                this.Subscriber.UnsubscribeAll();
                            }
                            catch ( ObjectDisposedException ) { }
                        }
                        else
                        {
                            // The connection has already been already disposed of.
                        }
                    }
                    else
                    {
                        // Don't attempt to touch the Subscriber during finalization.
                        // Redis resources are being finalized in the wrong order.
                    }

                    this._logger.Trace.IfEnabled?.Write( Formatted( "CompleteAdding." ) );
                    this._queueProcessingAllowedEvent.Set();
                    this._notificationQueue.Add( null );
                    this._notificationQueue.CompleteAdding();

                    // Messages are not enqueued from this point.

                    if ( this._notificationProcessingThread.IsAlive )
                    {
                        this._logger.Trace.IfEnabled?.Write( Formatted( "Waiting for the notification processing thread." ) );

                        if ( !this._notificationProcessingThreadCompleted.Task.Wait( 5000 ) )
                        {
                            this._logger.Warning.IfEnabled?.Write( Formatted( "The notification processing thread takes a long time to close." ) );
                            this._notificationProcessingThreadCompleted.Task.Wait();
                        }

                        this._logger.Trace.IfEnabled?.Write( Formatted( "Waiting for the notification processing thread: completed." ) );
                    }

                    // All messages are processed at this point.

                    this.ChangeStatus( Status.Disposed );

                    activity.SetSuccess();
                    this._disposeTask.SetResult( true );
                }
                catch ( Exception e )
                {
                    this._disposeTask.SetException( e );

                    throw;
                }
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }

        if ( disposing )
        {
            GC.SuppressFinalize( this );
        }
    }

    public async ValueTask DisposeAsync( CancellationToken cancellationToken = default )
    {
        using ( var activity = this._logger.Default.IfEnabled?.OpenAsyncActivity( Formatted( "DisposeAsync( queue: {Queue} )", this._id ) ) )
        {
            try
            {
                if ( !this.TryChangeStatus( Status.Default, Status.Disposing ) )
                {
                    activity?.SetOutcome( FlashtraceLevel.Debug, Formatted( "The method was already called." ) );
                    await this._disposeTask.Task;

                    return;
                }

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if ( this.Subscriber.Multiplexer.IsConnected )
                    {
                        try
                        {
                            this._logger.Trace.IfEnabled?.Write( Formatted( "Unsubscribing" ) );
                            await this.Subscriber.UnsubscribeAllAsync();
                        }
                        catch ( ObjectDisposedException ) { }
                    }
                    else
                    {
                        // The connection has already been already disposed of.
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    this._logger.Trace.IfEnabled?.Write( Formatted( "CompleteAdding." ) );
                    this._queueProcessingAllowedEvent.Set();
                    this._notificationQueue.Add( null, default );
                    this._notificationQueue.CompleteAdding();

                    if ( this._notificationProcessingThread.IsAlive )
                    {
                        // ReSharper disable once MethodSupportsCancellation
                        // ReSharper disable once BadPreprocessorIndent

#if NETCOREAPP
                        await
#endif
                        using ( cancellationToken.Register( () => this._notificationProcessingThreadCompleted.SetCanceled() ) )
                        {
                            this._logger.Trace.IfEnabled?.Write( Formatted( "Waiting for the notification processing thread." ) );
                            await this._notificationProcessingThreadCompleted.Task;
                            this._logger.Trace.IfEnabled?.Write( Formatted( "Waiting for the notification processing thread: completed." ) );

                            // All messages are processed at this point.\
                        }
                    }

                    this.ChangeStatus( Status.Disposed );

                    activity?.SetSuccess();
                    this._disposeTask.SetResult( true );
                }
                catch ( Exception e )
                {
                    this._disposeTask.SetException( e );

                    throw;
                }
            }
            catch ( Exception e ) when ( activity != null )
            {
                activity.Value.SetException( e );

                throw;
            }
        }

        GC.SuppressFinalize( this );
    }

    public int BackgroundTaskExceptions { get; private set; }

    /// <summary>
    /// Gets the default connection timeout when creating a Redis backend. 
    /// </summary>
    /// <remarks>
    /// When you change this value, please also change the documentation for <see cref="RedisCacheSynchronizerConfiguration.ConnectionTimeout"/>
    /// and <see cref="RedisCachingBackendConfiguration.ConnectionTimeout"/>.
    /// </remarks>
    public static TimeSpan DefaultSubscriptionTimeout { get; } = TimeSpan.FromMinutes( 1 );

    public override string ToString()
    {
        return string.Format( CultureInfo.InvariantCulture, "{{RedisNotificationQueue {0}}}", this._id );
    }

    private enum Status
    {
        Default,
        Suspended,
        Disposing,
        Disposed
    }
}