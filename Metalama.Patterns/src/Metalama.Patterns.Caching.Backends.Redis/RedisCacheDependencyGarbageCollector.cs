// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace;
using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using static Flashtrace.Messages.FormattedMessageBuilder;

namespace Metalama.Patterns.Caching.Backends.Redis;

[PublicAPI] // Comments above indicate use case.
public sealed class RedisCacheDependencyGarbageCollector : IHostedService, IDisposable, IAsyncDisposable
{
    private readonly RedisCachingBackendConfiguration _configuration;
    private readonly FlashtraceSource _logger;

    internal RedisNotificationQueueProcessor NotificationQueueProcessor { get; set; } = null!; // "Guaranteed" to be initialized via Init et al.

    private readonly bool _ownsBackend;
    private readonly DependenciesRedisCachingBackend _backend;

    internal RedisCacheDependencyGarbageCollector(
        RedisCachingBackendConfiguration configuration,
        IServiceProvider? serviceProvider )
    {
        this._configuration = configuration;
        this.ServiceProvider = serviceProvider;
        this._backend = new DependenciesRedisCachingBackend( c => c.GetDatabase( configuration.Database ), configuration, serviceProvider );
        this._ownsBackend = true;
        this._logger = serviceProvider.GetFlashtraceSource( this.GetType(), FlashtraceRole.Caching );
    }

    internal RedisCacheDependencyGarbageCollector( DependenciesRedisCachingBackend backend )
    {
        this._configuration = backend.Configuration;
        this._backend = backend;
        this._ownsBackend = false;
        this._logger = backend.ServiceProvider.GetFlashtraceSource( this.GetType(), FlashtraceRole.Caching );
    }

    private static RedisCachingBackend FindRedisCachingBackend( CachingBackend backend )
    {
        switch ( backend )
        {
            case RedisCachingBackend redisCachingBackend:
                return redisCachingBackend;

            case CachingBackendEnhancer enhancer:
                return FindRedisCachingBackend( enhancer.UnderlyingBackend );

            default:
                throw new ArgumentException( "The backend is not a Redis backend.", nameof(backend) );
        }
    }

    /// <summary>
    /// Gets the Redis <see cref="IDatabase"/> used by the current object.
    /// </summary>
    public IDatabase Database => this._backend.Database;

    private RedisKeyBuilder KeyBuilder => this._backend.KeyBuilder;

    private IServiceProvider? ServiceProvider { get; }

    /// <summary>
    /// Gets the Redis <see cref="IConnectionMultiplexer"/> used by the current object.
    /// </summary>
    internal IConnectionMultiplexer Connection => this._backend.Connection;

    private void ProcessKeyspaceNotification( RedisNotification notification )
    {
        string? channelName = notification.Channel;

        if ( channelName == null )
        {
            return;
        }

        var tokenizer = new StringTokenizer( channelName );

        // Was: `if ( tokenizer.GetNext() == null ) return;` - However, GetNext() never returns null, but does have side effects, so using discard.   
        _ = tokenizer.GetNext( ':' );

        var prefix = tokenizer.GetNext( ':' );

        if ( !prefix.Equals( this.KeyBuilder.KeyPrefix.AsSpan(), StringComparison.Ordinal ) )
        {
            return;
        }

        var keyKind = tokenizer.GetNext( ':' );

        var itemKey = tokenizer.GetRemainder().ToString();

        switch ( keyKind )
        {
            case RedisKeyBuilder.DependenciesKindPrefix:
                // When a dependencies key is removed by Redis (for whatever reason not under our control),
                // we need to remove the main key as well.
                switch ( notification.Value )
                {
                    case "expired":
                    case "evicted":
                        if ( this.Database.KeyDelete( this.KeyBuilder.GetValueKey( itemKey ) ) )
                        {
                            // ReSharper disable once StringLiteralTypo
                            this._logger.Warning.Write(
                                Formatted(
                                    "The dependencies key for item {Item} has been {State} but should not. The Redis server is probably misconfigured. " +
                                    "Only use volatile-* maxmemory policies.",
                                    itemKey,
                                    notification.Value ) );
                        }

                        // TODO: remove the main key from other dependencies.
                        break;
                }

                break;

            case RedisKeyBuilder.DependencyKindPrefix:
                // No idea of what to do with this situation.
                switch ( notification.Value )
                {
                    case "expired":
                    case "evicted":
                        // ReSharper disable once StringLiteralTypo
                        this._logger.Warning.Write(
                            Formatted(
                                "The dependency key {Key} has been {State} but should not. The Redis server is probably misconfigured. " +
                                "Only use volatile-* maxmemory policies.",
                                itemKey,
                                notification.Value ) );

                        break;
                }

                break;

            case RedisKeyBuilder.ValueKindPrefix:
                this._logger.Debug.IfEnabled?.Write( Formatted( "Enqueue processing of cache eviction." ) );
                this._backend.ExecuteNonBlockingTask( ct => this.OnValueEvictedAsync( itemKey, ct ) );

                break;

            default:
                this._logger.Debug.IfEnabled?.Write( Formatted( "Notification ignored." ) );

                break;
        }
    }

    private async Task OnValueEvictedAsync( string key, CancellationToken cancellationToken )
    {
        string? valueKey = this.KeyBuilder.GetValueKey( key );
        string? dependenciesKey = this.KeyBuilder.GetDependenciesKey( key );

        for ( var attempt = 0; attempt < this._backend.Configuration.TransactionMaxRetries + 1; attempt++ )
        {
            var valueKeyExistsTask = this.Database.KeyExistsAsync( valueKey );
            var dependenciesKeyExistsTask = this.Database.KeyExistsAsync( dependenciesKey );

            await Task.WhenAll( valueKeyExistsTask, dependenciesKeyExistsTask );

            // This condition repeats in the following transaction.
            // It is more expensive to set up the transaction and we know that just one client out of all clients
            // connected will be able to execute the transaction, so we stop all the other clients here.
            if (

                // The value has been set again in the meanwhile
                valueKeyExistsTask.Result

                // The garbage collection has been performed by another client already
                || !dependenciesKeyExistsTask.Result )
            {
                return;
            }

            var transaction = this.Database.CreateTransaction();
            transaction.AddCondition( Condition.KeyNotExists( valueKey ) );
            transaction.AddCondition( Condition.KeyExists( dependenciesKey ) );
            var dependencies = await this._backend.GetDependenciesAsync( key, transaction );
            this._backend.RemoveDependenciesTransaction( key, dependencies, transaction );
#pragma warning disable 4014
            transaction.KeyDeleteAsync( dependenciesKey );
#pragma warning restore 4014
            if ( await transaction.ExecuteAsync() )
            {
                return;
            }
        }

        throw new CachingException( DependenciesRedisCachingBackend.TooManyTransactionAttemptsErrorMessage );
    }

    /// <summary>
    /// Disposes the current object.
    /// </summary>
    public void Dispose()
    {
        this.NotificationQueueProcessor.Dispose();

        if ( this._ownsBackend )
        {
            this._backend.Dispose();
        }
    }

    /// <summary>
    /// Asynchronously disposes the current object.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public async ValueTask DisposeAsync( CancellationToken cancellationToken )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await this.NotificationQueueProcessor.DisposeAsync( cancellationToken );

        cancellationToken.ThrowIfCancellationRequested();

        if ( this._ownsBackend )
        {
            await this._backend.DisposeAsync( cancellationToken );
        }
    }

    internal int BackgroundTaskExceptions => this.NotificationQueueProcessor.BackgroundTaskExceptions;

    Task IHostedService.StartAsync( CancellationToken cancellationToken ) => this.InitializeAsync( cancellationToken );

    Task IHostedService.StopAsync( CancellationToken cancellationToken ) => this.DisposeAsync( cancellationToken ).AsTask();

    public void Initialize()
    {
        if ( this._ownsBackend )
        {
            this._backend.Initialize();
        }
        else if ( this._backend.Status == CachingBackendStatus.Default )
        {
            throw new InvalidOperationException( "The back-end is neither owned nor initialized." );
        }

        // ReSharper disable once RedundantSuppressNullableWarningExpression
        this.NotificationQueueProcessor = RedisNotificationQueueProcessor.Create(
            this.ToString()!,
            this.Connection,
            [this.KeyBuilder.NotificationChannel],
            this.ProcessKeyspaceNotification,
            this._configuration.ConnectionTimeout,
            this.ServiceProvider );
    }

    public async Task InitializeAsync( CancellationToken cancellationToken = default )
    {
        if ( this._ownsBackend )
        {
            await this._backend.InitializeAsync( cancellationToken );
        }
        else if ( this._backend.Status == CachingBackendStatus.Default )
        {
            throw new InvalidOperationException( "The back-end is neither owned nor initialized." );
        }

        // ReSharper disable once RedundantSuppressNullableWarningExpression
        this.NotificationQueueProcessor = await RedisNotificationQueueProcessor.CreateAsync(
            this.ToString()!,
            this.Connection,
            [this.KeyBuilder.NotificationChannel],
            this.ProcessKeyspaceNotification,
            this._configuration.ConnectionTimeout,
            this.ServiceProvider,
            cancellationToken );
    }

    public async Task PerformFullCollectionAsync( CancellationToken cancellationToken = default )
    {
        await this._backend.InitializeAsync( cancellationToken );
        await this._backend.CleanUpAsync( cancellationToken );
    }

    public ValueTask DisposeAsync() => this.DisposeAsync( default );
}