// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Metalama.Patterns.Caching.Backends.Redis;

/// <summary>
/// An implementation of <see cref="CacheSynchronizer"/>  that uses Redis publish/subscribe channels to invalidate several
/// instances of local caches.
/// </summary>
[PublicAPI]
internal sealed class RedisCacheSynchronizer : CacheSynchronizer
{
    private readonly bool _ownsConnection;
    private readonly RedisChannel _channel;
    private readonly TimeSpan _connectionTimeout;

    private IConnectionMultiplexer? _connection;

    private RedisNotificationQueueProcessor NotificationQueueProcessor { get; set; } = null!; // "Guaranteed" to be initialized via Init et al.

    /// <summary>
    /// Gets the Redis <see cref="IConnectionMultiplexer"/> used by the current <see cref="RedisCacheSynchronizer"/>.
    /// </summary>
    private IConnectionMultiplexer Connection => this._connection ?? throw new InvalidOperationException( "The component is not initialized." );

    public RedisCacheSynchronizer(
        CachingBackend underlyingBackend,
        RedisCacheSynchronizerConfiguration configuration ) : base( underlyingBackend, configuration )
    {
        this._ownsConnection = configuration.OwnsConnection;
        this._connectionTimeout = configuration.ConnectionTimeout;
        this._channel = new RedisChannel( configuration.ChannelName, RedisChannel.PatternMode.Literal );
    }

    protected override void InitializeCore()
    {
        var redisConnectionFactory = ((RedisCacheSynchronizerConfiguration) this.Configuration).ConnectionFactory;

        this._connection = redisConnectionFactory != null!
            ? redisConnectionFactory.GetConnection( this.ServiceProvider )
            : this.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        this.NotificationQueueProcessor = RedisNotificationQueueProcessor.Create(
            this.ToString(),
            this.Connection,
            [this._channel],
            this.HandleMessage,
            this._connectionTimeout,
            this.ServiceProvider );

        base.InitializeCore();
    }

    protected override async Task InitializeCoreAsync( CancellationToken cancellationToken = default )
    {
        var configuration = (RedisCacheSynchronizerConfiguration) this.Configuration;

        if ( configuration.ConnectionFactory != null! )
        {
            this._connection =
                await configuration.ConnectionFactory.GetConnectionAsync(
                    this.ServiceProvider,
                    configuration.LogRedisConnection,
                    cancellationToken );
        }
        else
        {
            this._connection = this.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        }

        this.NotificationQueueProcessor = await RedisNotificationQueueProcessor.CreateAsync(
            this.ToString(),
            this.Connection,
            [this._channel],
            this.HandleMessage,
            this._connectionTimeout,
            this.ServiceProvider,
            cancellationToken );

        await base.InitializeCoreAsync( cancellationToken );
    }

    private void HandleMessage( RedisNotification notification )
    {
        string? notificationValue = notification.Value;

        if ( notificationValue != null )
        {
            this.OnMessageReceived( notificationValue );
        }
    }

    /// <inheritdoc />
    protected override async Task SendMessageAsync( string message, CancellationToken cancellationToken )
    {
        await this.NotificationQueueProcessor.Subscriber.PublishAsync( this._channel, message );
    }

    /// <inheritdoc />
    protected override void DisposeCore( bool disposing, CancellationToken cancellationToken )
    {
        base.DisposeCore( disposing, cancellationToken );

        this.NotificationQueueProcessor.Dispose();

        if ( this._ownsConnection )
        {
            this.Connection.Close();
            this.Connection.Dispose();
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore( CancellationToken cancellationToken )
    {
        await base.DisposeAsyncCore( cancellationToken );

        await this.NotificationQueueProcessor.DisposeAsync( cancellationToken );

        if ( this._ownsConnection )
        {
            await this.Connection.CloseAsync();
            this.Connection.Dispose();
        }
    }
}