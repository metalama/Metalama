// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;
using StackExchange.Redis;

namespace Metalama.Patterns.Caching.Backends.Redis;

#pragma warning disable SA1623

/// <summary>
/// Options for <see cref="RedisCacheSynchronizer"/>.
/// </summary>
/// <remarks>
/// <para>By default, the <see cref="IConnectionMultiplexer"/> is retrieved from the <see cref="IServiceProvider"/>.
/// To define another way to get the <see cref="IConnectionMultiplexer"/>, set the <see cref="Connection"/> or <see cref="NewConnectionOptions"/> properties.</para>
/// </remarks>
[PublicAPI]
public sealed record RedisCacheSynchronizerConfiguration : CacheSynchronizerConfiguration
{
    private IConnectionMultiplexer? _connection;
    private ConfigurationOptions? _configurationOptions;

    internal IRedisConnectionFactory ConnectionFactory { get; init; } = ServiceProviderRedisConnectionFactory.Instance;

    public RedisCacheSynchronizerConfiguration() { }

    [Obsolete( "Use the default constructor and use RedisCacheSynchronizerBuilder.WithNewRedisConnection." )]
    public RedisCacheSynchronizerConfiguration( ConfigurationOptions redisConnectionOptions, string? prefix = null )
    {
        this.NewConnectionOptions = redisConnectionOptions;
        this.Prefix = prefix ?? DefaultPrefix;
    }

    [Obsolete( "Use the default constructor and use RedisCacheSynchronizerBuilder.WithRedisConnection." )]
    public RedisCacheSynchronizerConfiguration( IConnectionMultiplexer connection, string? prefix = null )
    {
        this.Connection = connection;
        this.Prefix = prefix ?? DefaultPrefix;
    }

    /// <summary>
    /// Gets or sets the <see cref="IConnectionMultiplexer"/> that will be used by the <see cref="RedisCacheSynchronizer"/>.
    /// </summary>
    /// <remarks>
    /// <para>Setting this property resets the <see cref="NewConnectionOptions"/> property.</para>
    /// </remarks>
    public IConnectionMultiplexer? Connection
    {
        get => this._connection;
        init
        {
            this._connection = value;

            if ( value != null )
            {
                this.ConnectionFactory = new ExistingRedisConnectionFactory( value );
                this._configurationOptions = null;
            }
            else
            {
                this.ConnectionFactory = ServiceProviderRedisConnectionFactory.Instance;
            }
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="ConfigurationOptions"/> that will be used to create a new <see cref="ConnectionMultiplexer"/> for use by
    /// the new <see cref="RedisCacheSynchronizer"/>.
    /// </summary>
    /// <remarks>
    /// <para>Setting this property resets the <see cref="Connection"/> property.</para>
    /// </remarks>
    public ConfigurationOptions? NewConnectionOptions
    {
        get => this._configurationOptions;
        init
        {
            this._configurationOptions = value;

            if ( value != null )
            {
                this.ConnectionFactory = new NewRedisConnectionFactory( value );
                this.OwnsConnection = true;
                this._connection = null;
            }
            else
            {
                this.ConnectionFactory = ServiceProviderRedisConnectionFactory.Instance;
            }
        }
    }

    /// <summary>
    /// Gets or sets the name of the Redis channel to use to exchange invalidation messages. The default value is <c>RedisCacheInvalidator</c>.
    /// </summary>
    public string ChannelName { get; init; } = nameof(RedisCacheSynchronizer);

    /// <summary>
    /// Gets or sets a value indicating whether determines whether disposing the <see cref="RedisCacheSynchronizer"/> also disposes the Redis connection. The default value is <c>false</c>.
    /// </summary>
    public bool OwnsConnection { get; init; }

    /// <summary>
    /// Gets or sets the time that the Redis invalidator will wait for a Redis connection.
    /// (When you create a new Redis invalidator, if it doesn't connect to a Redis server in this timeout, a <see cref="TimeoutException"/> is thrown.)
    /// </summary>
    /// <remarks>
    /// The default value is 1 minute.
    /// </remarks>
    public TimeSpan ConnectionTimeout { get; init; } = RedisNotificationQueueProcessor.DefaultSubscriptionTimeout;

    /// <summary>
    /// Gets or sets a value indicating whether the logs of the <see cref="ConnectionMultiplexer"/> should be captured and
    /// redirected. The default value is <c>false</c>. 
    /// </summary>
    public bool LogRedisConnection { get; init; }
}