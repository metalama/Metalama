// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.Serializers;
using StackExchange.Redis;

namespace Metalama.Patterns.Caching.Backends.Redis;

#pragma warning disable SA1623

/// <summary>
/// Configuration for <see cref="RedisCachingBackend"/>.
/// </summary>
/// <remarks>
/// <para>By default, the <see cref="IConnectionMultiplexer"/> is retrieved from the <see cref="IServiceProvider"/>.
/// To define another way to get the <see cref="IConnectionMultiplexer"/>, set the <see cref="Connection"/> or <see cref="NewConnectionOptions"/> properties.</para>
/// </remarks>
[PublicAPI]
public record RedisCachingBackendConfiguration : CachingBackendConfiguration
{
    private string? _keyPrefix;
    private ConfigurationOptions? _configurationOptions;
    private IConnectionMultiplexer? _connection;

    internal IRedisConnectionFactory ConnectionFactory { get; init; } = ServiceProviderRedisConnectionFactory.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCachingBackendConfiguration"/> class.
    /// </summary>
    public RedisCachingBackendConfiguration() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCachingBackendConfiguration"/> class that
    /// creates a new <see cref="IConnectionMultiplexer"/>.
    /// </summary>
    [Obsolete( "Use the default constructor and the NewConnectionOptions property." )]
    public RedisCachingBackendConfiguration( ConfigurationOptions redisConnectionOptions, string? keyPrefix = null )
    {
        this.NewConnectionOptions = redisConnectionOptions;
        this.KeyPrefix = keyPrefix;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCachingBackendConfiguration"/> class that
    /// with a given <see cref="IConnectionMultiplexer"/>.
    /// </summary>
    [Obsolete( "Use the default constructor and the Connection ." )]
    public RedisCachingBackendConfiguration( IConnectionMultiplexer connection, ConfigurationOptions? configurationOptions, string? keyPrefix = null )
    {
        this._configurationOptions = configurationOptions;
        this.Connection = connection;
        this.KeyPrefix = keyPrefix;
    }

    /// <summary>
    /// Gets or sets the <see cref="IConnectionMultiplexer"/> that will be used by the Redis <see cref="CachingBackend"/>.
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
    /// the new Redis <see cref="CachingBackend"/>.
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
                this._connection = null;
                this.OwnsConnection = true;
            }
            else
            {
                this.ConnectionFactory = ServiceProviderRedisConnectionFactory.Instance;
            }
        }
    }

    /// <summary>
    /// Gets or sets the prefix for the key of all Redis items created by the <see cref="RedisCachingBackend"/>. The default value is <c>cache</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     It is a good practice to include the version of your application in the key prefix. In the likely situation where several
    /// versions of the application are running simultaneously, having a distinct <see cref="KeyPrefix"/> can avoid deserialization issues when one
    /// cached item is serialized with one version but deserialized with a different version.
    /// </para>
    /// </remarks>
    public string? KeyPrefix
    {
        get => this._keyPrefix;
        init
        {
            if ( value != null
#if NETFRAMEWORK || NETSTANDARD
                 && value.IndexOf( ":", StringComparison.Ordinal ) != -1
#else
                 && value.Contains( ":", StringComparison.Ordinal )
#endif
               )
            {
                throw new ArgumentOutOfRangeException( nameof(value), "The KeyPrefix property value cannot contain the ':' character." );
            }

            this._keyPrefix = value;
        }
    }

    /// <summary>
    /// Gets or sets the index of the database to use. The default value is <c>-1</c> (automatic selection).
    /// </summary>
    public int Database { get; init; } = -1;

    /// <summary>
    /// Gets or sets a function that creates the serializer used to serialize objects into byte arrays (and conversely).
    /// The default value is <c>null</c>, which means that <see cref="JsonCachingSerializer"/> will be used.
    /// </summary>
    public Func<ICachingSerializer>? CreateSerializer { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether determines whether the <see cref="RedisCachingBackend"/> should dispose the Redis connection when the <see cref="RedisCachingBackend"/>
    /// itself is disposed. The default value is <c>false</c>.
    /// </summary>
    public bool OwnsConnection { get; init; }

    /// <summary>
    /// Gets or sets the number of times Redis transactions are retried when they fail due to a data conflict, before an exception
    /// is raised. The default value is <c>5</c>.
    /// </summary>
    public int TransactionMaxRetries { get; init; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="RedisCachingBackend"/> should support dependencies. When this property is used,
    /// the <see cref="DependenciesRedisCachingBackend"/> class is used instead of <see cref="RedisCachingBackend"/>. Note that when dependencies
    /// are enabled, at least one instance of the Redis garbage collection must run. See <see cref="RunGarbageCollector"/> for details.
    /// </summary>
    public bool SupportsDependencies { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the dependency garbage collector process should run while the caching back-end instance
    /// is alive. This property only makes sense when <see cref="SupportsDependencies"/> is set to <c>true</c>. The value of <see cref="RunGarbageCollector"/>
    /// is <c>false</c> by default. It is necessary to have at least one instance of the garbage collector active at any time. It is allowed but useless to
    /// have several concurrent instances of the garbage collector, but having a large number of them can hurt performance of the Redis server.
    /// The recommended approach to run the garbage collector is to deploy a separate application that only hosts this service. See <see cref="RedisCachingFactory.CreateRedisCacheDependencyGarbageCollector"/>
    /// for details.
    /// </summary>
    public bool RunGarbageCollector { get; init; }

    /// <summary>
    /// Gets or sets the default expiration time of cached items.
    /// All items that don't have an explicit expiration time are automatically expired according to the value
    /// of this property, unless they have the <see cref="CacheItemPriority.NotRemovable"/> priority.
    /// The default value is 1 day.
    /// </summary>
    public TimeSpan DefaultExpiration { get; init; } = TimeSpan.FromDays( 1 );

    /// <summary>
    /// Gets the time that the Redis backend will wait for a Redis connection.
    /// (When you create a new Redis backend, if it doesn't connect to a Redis server in this timeout, a <see cref="TimeoutException"/> is thrown.)
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

    /// <summary>
    /// Gets or sets the Redis <see cref="CommandFlags"/> for read-only operations. The default value is <see cref="CommandFlags.PreferReplica"/>.
    /// </summary>
    public CommandFlags ReadCommandFlags { get; init; } = CommandFlags.PreferReplica;

    /// <summary>
    /// Gets or sets the Redis <see cref="CommandFlags"/> for write operations. The default value is <see cref="CommandFlags.PreferMaster"/>.
    /// </summary>
    public CommandFlags WriteCommandFlags { get; init; } = CommandFlags.PreferMaster;
}