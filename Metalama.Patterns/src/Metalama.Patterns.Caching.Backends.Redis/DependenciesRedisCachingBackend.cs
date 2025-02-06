// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Implementation;
using StackExchange.Redis;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using static Flashtrace.Messages.FormattedMessageBuilder;

namespace Metalama.Patterns.Caching.Backends.Redis;

internal sealed class DependenciesRedisCachingBackend : RedisCachingBackend
{
    private const char _dependenciesSeparator = '\n';
    private const int _itemVersionIndex = 0;
    private const int _itemValueIndex = 1;
    private const int _itemVersionInDependenciesIndex = 0;
    private const int _firstDependencyIndex = 1;
    private const string _noDependencyVersion = "-";
    private const string _tooManyGetItemAttemptsErrorMessage = "Too many get-item attempts.";
    internal const string TooManyTransactionAttemptsErrorMessage = "Too many transaction attempts.";

    private const string _dependencyInvalidatedEvent = "dependency";
    private const string _itemInvalidatedEvent = "item-invalidated";

    private static readonly string _dependenciesSeparatorString = new( _dependenciesSeparator, 1 );

    internal DependenciesRedisCachingBackend(
        RedisCachingBackendConfiguration configuration,
        IServiceProvider? serviceProvider )
        : base( configuration, serviceProvider ) { }

    internal DependenciesRedisCachingBackend(
        Func<IConnectionMultiplexer, IDatabase> databaseFactory,
        RedisCachingBackendConfiguration configuration,
        IServiceProvider? serviceProvider )
        : base( databaseFactory, configuration, serviceProvider ) { }

    protected override CachingBackendFeatures CreateFeatures() => new DependenciesRedisCachingBackendFeatures();

    internal RedisCacheDependencyGarbageCollector? Collector { get; set; }

    private string[]? GetDependencies( string key, ITransaction? transaction = null )
    {
        var dependenciesKey = this.KeyBuilder.GetDependenciesKey( key );
        string? dependencies = this.Database.StringGet( dependenciesKey );

        transaction?.AddCondition(
            dependencies == null
                ? Condition.KeyNotExists( dependenciesKey )
                : Condition.StringEqual( dependenciesKey, dependencies ) );

        return dependencies?.Split( _dependenciesSeparator );
    }

    protected override void InitializeCore()
    {
        base.InitializeCore();
        this.Collector?.Initialize();
    }

    protected override async Task InitializeCoreAsync( CancellationToken cancellationToken = default )
    {
        await base.InitializeCoreAsync( cancellationToken );

        if ( this.Collector != null )
        {
            await this.Collector.InitializeAsync( cancellationToken );
        }
    }

    protected override void DisposeCore( bool disposing, CancellationToken cancellationToken )
    {
        this.Collector?.Dispose();
        base.DisposeCore( disposing, cancellationToken );
    }

    protected override async ValueTask DisposeAsyncCore( CancellationToken cancellationToken )
    {
        if ( this.Collector != null )
        {
            await this.Collector.DisposeAsync( cancellationToken );
        }

        await base.DisposeAsyncCore( cancellationToken );
    }

    internal async Task<string[]?> GetDependenciesAsync( string key, ITransaction? transaction = null )
    {
        var dependenciesKey = this.KeyBuilder.GetDependenciesKey( key );
        string? dependencies = await this.Database.StringGetAsync( dependenciesKey, this.Configuration.ReadCommandFlags );

        transaction?.AddCondition(
            dependencies == null
                ? Condition.KeyNotExists( dependenciesKey )
                : Condition.StringEqual( dependenciesKey, dependencies ) );

        return dependencies?.Split( _dependenciesSeparator );
    }

    protected override bool ProcessEvent( string kind, string key, Guid sourceId )
    {
        if ( base.ProcessEvent( kind, key, sourceId ) )
        {
            return true;
        }

        switch ( kind )
        {
            case _dependencyInvalidatedEvent:
                this.OnDependencyInvalidated( key, sourceId );

                return true;

            case _itemInvalidatedEvent:
                this.OnItemRemoved( key, CacheItemRemovedReason.Invalidated, sourceId );

                return true;
        }

        return false;
    }

    /// <inheritdoc />
    protected override async Task DeleteItemAsync( string key )
    {
        for ( var attempt = 0; attempt < this.Configuration.TransactionMaxRetries + 1; attempt++ )
        {
            var transaction = this.Database.CreateTransaction();
            var dependencies = await this.GetDependenciesAsync( key, transaction );
            this.DeleteItemTransaction( key, dependencies, transaction );

            if ( await transaction.ExecuteAsync( this.Configuration.WriteCommandFlags ) )
            {
                return;
            }
            else
            {
                this.LogSource.Debug.Write( Formatted( "Transaction DeleteItem failed. Retrying." ) );
            }
        }

        throw new CachingException( TooManyTransactionAttemptsErrorMessage );
    }

    private void LogRetryTransaction( [CallerMemberName] string? memberName = null )
    {
        this.LogSource.Debug.Write( Formatted( "Transaction {Method} failed. Retrying.", memberName ) );
    }

    /// <inheritdoc />
    protected override void DeleteItem( string key )
    {
        for ( var attempt = 0; attempt < this.Configuration.TransactionMaxRetries + 1; attempt++ )
        {
            var transaction = this.Database.CreateTransaction();
            var dependencies = this.GetDependencies( key, transaction );
            this.DeleteItemTransaction( key, dependencies, transaction );

            if ( transaction.Execute( this.Configuration.WriteCommandFlags ) )
            {
                return;
            }
            else
            {
                this.LogRetryTransaction();
            }
        }

        throw new CachingException( TooManyTransactionAttemptsErrorMessage );
    }

    private void DeleteItemTransaction( string key, string[]? dependencies, ITransaction transaction )
    {
        var dependenciesKey = this.KeyBuilder.GetDependenciesKey( key );
        var valueKey = this.KeyBuilder.GetValueKey( key );

        if ( dependencies is { Length: > _firstDependencyIndex } )
        {
            this.RemoveDependenciesTransaction( key, dependencies, transaction );
        }

        this.LogSource.Debug.Write( Formatted( "KeyDelete({Key})", dependenciesKey ) );
        transaction.KeyDeleteAsync( dependenciesKey );
        this.LogSource.Debug.Write( Formatted( "KeyDelete({Key})", valueKey ) );
        transaction.KeyDeleteAsync( valueKey );
    }

    internal void RemoveDependenciesTransaction( string key, string[]? dependencies, ITransaction transaction )
    {
        if ( dependencies != null )
        {
            for ( var i = _firstDependencyIndex; i < dependencies.Length; i++ )
            {
                this.RemoveDependencyAsync( key, dependencies[i], transaction );
            }
        }
    }

    private Task RemoveDependencyAsync( string valueKey, string dependency, IDatabaseAsync database )
    {
        var dependencyKey = this.KeyBuilder.GetDependencyKey( dependency );
        this.LogSource.Debug.Write( Formatted( "SetRemove({DependencyKey}, {ValueKey})", dependencyKey, valueKey ) );

        return database.SetRemoveAsync( dependencyKey, valueKey, this.Configuration.WriteCommandFlags );
    }

    private static TimeSpan? CreateExpiry( CacheItem policy )
    {
        // TODO: make this configurable
        TimeSpan? ttl = TimeSpan.FromDays( 1 );

        if ( policy.Configuration != null )
        {
            if ( policy.Configuration.AbsoluteExpiration.HasValue )
            {
                ttl = policy.Configuration.AbsoluteExpiration;
            }
            else if ( policy.Configuration.SlidingExpiration.HasValue )
            {
                ttl = policy.Configuration.SlidingExpiration.Value;
            }
            else if ( policy.Configuration.Priority.GetValueOrDefault() == CacheItemPriority.NotRemovable )
            {
                ttl = null;
            }
        }

        return ttl;
    }

    /// <inheritdoc />
    protected override void SetItemCore( string key, CacheItem item )
    {
        var valueKey = this.KeyBuilder.GetValueKey( key );

        for ( var attempt = 0; attempt < this.Configuration.TransactionMaxRetries + 1; attempt++ )
        {
            var transaction = this.Database.CreateTransaction();

            var version = this.Database.ListGetByIndex( valueKey, _itemVersionIndex, this.Configuration.ReadCommandFlags );

            if ( version.HasValue )
            {
                // There may be a race between two threads trying to set the same key with different values.
                // By design (i.e., because we don't support distributed locks), it does not matter which thread wins.
                // However, we want to prevent corrupted entries, i.e. we want to ensure that the value/dependency/dependencies
                // keys are all consistent. This is why we're using the item versioning thing.

                this.LogSource.Debug.Write(
                    Formatted(
                        "Version of existing item is {Version}. Deleting the key {Key} under the condition that the version remains equal.",
                        version,
                        valueKey ) );

                transaction.AddCondition( Condition.ListIndexEqual( valueKey, _itemVersionIndex, version ) );
                transaction.KeyDeleteAsync( valueKey );

                if ( version != _noDependencyVersion )
                {
                    var dependencies = this.GetDependencies( key, transaction );
                    this.RemoveDependenciesTransaction( key, dependencies, transaction );
                }
            }
            else
            {
                transaction.AddCondition( Condition.KeyNotExists( valueKey ) );
            }

            this.SetItemTransaction( key, item, transaction );

            if ( transaction.Execute( this.Configuration.WriteCommandFlags ) )
            {
                return;
            }
            else
            {
                this.LogRetryTransaction();
            }
        }

        throw new CachingException( TooManyTransactionAttemptsErrorMessage );
    }

    /// <inheritdoc />
    protected override async ValueTask SetItemAsyncCore( string key, CacheItem item, CancellationToken cancellationToken )
    {
        var valueKey = this.KeyBuilder.GetValueKey( key );

        for ( var attempt = 0; attempt < this.Configuration.TransactionMaxRetries + 1; attempt++ )
        {
            var transaction = this.Database.CreateTransaction();

            var version = await this.Database.ListGetByIndexAsync( valueKey, _itemVersionIndex, this.Configuration.ReadCommandFlags );

            if ( version.HasValue )
            {
                // There may be a race between two threads trying to set the same key with different values.
                // By design (i.e., because we don't support distributed locks), it does not matter which thread wins.
                // However, we want to prevent corrupted entries, i.e. we want to ensure that the value/dependency/dependencies
                // keys are all consistent. This is why we're using the item versioning thing.

                this.LogSource.Debug.Write(
                    Formatted(
                        "Version of existing item is {Version}. Deleting the key {Key} under the condition that the version remains equal.",
                        version,
                        valueKey ) );

                transaction.AddCondition( Condition.ListIndexEqual( valueKey, _itemVersionIndex, version ) );
#pragma warning disable 4014
                transaction.KeyDeleteAsync( valueKey );
#pragma warning restore 4014

                if ( version != _noDependencyVersion )
                {
                    var dependencies = this.GetDependencies( key, transaction );
                    this.RemoveDependenciesTransaction( key, dependencies, transaction );
                }
            }
            else
            {
                transaction.AddCondition( Condition.KeyNotExists( valueKey ) );
            }

            this.SetItemTransaction( key, item, transaction );

            if ( await transaction.ExecuteAsync( this.Configuration.WriteCommandFlags ) )
            {
                return;
            }
            else
            {
                this.LogSource.Debug.Write( Formatted( "Transaction SetItem failed. Retrying." ) );
            }
        }

        throw new CachingException( TooManyTransactionAttemptsErrorMessage );
    }

    private void SetItemTransaction( string key, CacheItem item, ITransaction transaction )
    {
        var value = this.Serialize( item );

        var valueKey = this.KeyBuilder.GetValueKey( key );

        var expiry = CreateExpiry( item );

        string version;

        if ( item.Dependencies.IsDefaultOrEmpty )
        {
            version = _noDependencyVersion;
        }
        else
        {
            // Store dependencies.
            foreach ( var dependency in item.Dependencies )
            {
                var dependencyKey = this.KeyBuilder.GetDependencyKey( dependency );
                transaction.SetAddAsync( dependencyKey, key );
            }

            // Generate unique identifier of the value
            version = Guid.NewGuid().ToString();
            var dependenciesString = version + _dependenciesSeparator + string.Join( _dependenciesSeparatorString, item.Dependencies );
            transaction.StringSetAsync( this.KeyBuilder.GetDependenciesKey( key ), dependenciesString );
        }

        // Store the item itself.
        transaction.ListRightPushAsync( valueKey, [version, value] );
        transaction.KeyExpireAsync( valueKey, expiry );
    }

    /// <inheritdoc />
    protected override CacheItem? GetItemCore( string key, bool includeDependencies )
    {
        var valueKey = this.KeyBuilder.GetValueKey( key );
        RedisValue[] itemList;
        string[]? dependencies = null;

        for ( var attempt = 0; attempt < this.Configuration.TransactionMaxRetries + 1; attempt++ )
        {
            itemList = this.Database.ListRange( valueKey, flags: this.Configuration.ReadCommandFlags );

            if ( itemList == null! || itemList.Length == 0 )
            {
                return null;
            }

            if ( !includeDependencies || itemList[_itemVersionIndex] == _noDependencyVersion )
            {
                goto returnItem;
            }

            dependencies = this.GetDependencies( key );

            if ( dependencies != null && dependencies[_itemVersionInDependenciesIndex] == (string?) itemList[_itemVersionIndex] )
            {
                goto returnItem;
            }
        }

        throw new CachingException( _tooManyGetItemAttemptsErrorMessage );

    returnItem:

        var dependencyArray = dependencies == null ? default : ImmutableArray.Create( dependencies, _firstDependencyIndex, dependencies.Length - 1 );

        return this.DeserializeAndExpire( valueKey, itemList[_itemValueIndex], dependencyArray );
    }

    /// <inheritdoc />
    protected override async ValueTask<CacheItem?> GetItemAsyncCore( string key, bool includeDependencies, CancellationToken cancellationToken )
    {
        var valueKey = this.KeyBuilder.GetValueKey( key );
        RedisValue[] itemList;
        string[]? dependencies = null;

        for ( var attempt = 0; attempt < this.Configuration.TransactionMaxRetries + 1; attempt++ )
        {
            itemList = await this.Database.ListRangeAsync( valueKey, flags: this.Configuration.ReadCommandFlags );

            if ( itemList == null! || itemList.Length == 0 )
            {
                return null;
            }

            if ( !includeDependencies || itemList[_itemVersionIndex] == _noDependencyVersion )
            {
                goto returnItem;
            }

            dependencies = await this.GetDependenciesAsync( key );

            if ( dependencies != null && dependencies[_itemVersionInDependenciesIndex] == (string?) itemList[_itemVersionIndex] )
            {
                goto returnItem;
            }
        }

        throw new CachingException( _tooManyGetItemAttemptsErrorMessage );

    returnItem:
        var dependencyArray = dependencies == null ? default : ImmutableArray.Create( dependencies, _firstDependencyIndex, dependencies.Length - 1 );

        return this.DeserializeAndExpire( valueKey, itemList[_itemValueIndex], dependencyArray );
    }

    /// <inheritdoc />
    protected override bool ContainsDependencyCore( string key )
    {
        return this.Database.KeyExists( this.KeyBuilder.GetDependencyKey( key ), this.Configuration.ReadCommandFlags );
    }

    /// <inheritdoc />
    protected override async ValueTask<bool> ContainsDependencyAsyncCore( string key, CancellationToken cancellationToken )
    {
        return await this.Database.KeyExistsAsync( this.KeyBuilder.GetDependencyKey( key ), this.Configuration.ReadCommandFlags );
    }

    /// <inheritdoc />
    protected override async ValueTask InvalidateDependencyAsyncCore( string key, CancellationToken cancellationToken )
    {
        var dependencyKey = this.KeyBuilder.GetDependencyKey( key );

        for ( var attempt = 0; attempt < this.Configuration.TransactionMaxRetries + 1; attempt++ )
        {
            var dependentItemKeys = await this.Database.SetMembersAsync( dependencyKey, this.Configuration.WriteCommandFlags );

            if ( dependentItemKeys is not { Length: > 0 } )
            {
                // There is nothing to invalidate.
                return;
            }

            var transaction = this.Database.CreateTransaction();

            foreach ( var dependentItemKey in dependentItemKeys )
            {
                string? dependentItemKeyString = dependentItemKey;

                if ( dependentItemKeyString == null )
                {
                    continue;
                }

                var dependencies = await this.GetDependenciesAsync( dependentItemKeyString, transaction );
                this.DeleteItemTransaction( dependentItemKeyString, dependencies, transaction );
            }

            if ( await transaction.ExecuteAsync( this.Configuration.WriteCommandFlags ) )
            {
                // Send notifications.
                await this.SendEventAsync( _dependencyInvalidatedEvent, key );

                foreach ( var dependentItemKey in dependentItemKeys )
                {
                    string? dependentItemKeyString = dependentItemKey;

                    if ( dependentItemKeyString == null )
                    {
                        continue;
                    }

                    await this.SendEventAsync( _itemInvalidatedEvent, dependentItemKeyString );
                }

                return;
            }
            else
            {
                this.LogSource.Debug.Write( Formatted( "Transaction InvalidateDependency failed. Retrying." ) );
            }
        }

        throw new CachingException( TooManyTransactionAttemptsErrorMessage );
    }

    protected override void InvalidateDependencyCore( string key )
    {
        var dependencyKey = this.KeyBuilder.GetDependencyKey( key );

        for ( var attempt = 0; attempt < this.Configuration.TransactionMaxRetries + 1; attempt++ )
        {
            var dependentItemKeys = this.Database.SetMembers( dependencyKey, this.Configuration.WriteCommandFlags );

            if ( dependentItemKeys is not { Length: > 0 } )
            {
                // There is nothing to invalidate.
                return;
            }

            var transaction = this.Database.CreateTransaction();

            foreach ( var dependentItemKey in dependentItemKeys )
            {
                string? dependentItemKeyString = dependentItemKey;

                if ( dependentItemKeyString == null )
                {
                    continue;
                }

                var dependencies = this.GetDependencies( dependentItemKeyString, transaction );
                this.DeleteItemTransaction( dependentItemKeyString, dependencies, transaction );
            }

            if ( transaction.Execute( this.Configuration.WriteCommandFlags ) )
            {
                // Send notifications.
                this.SendEvent( _dependencyInvalidatedEvent, key );

                foreach ( var dependentItemKey in dependentItemKeys )
                {
                    string? dependentItemKeyString = dependentItemKey;

                    if ( dependentItemKeyString == null )
                    {
                        continue;
                    }

                    this.SendEvent( _itemInvalidatedEvent, dependentItemKeyString );
                }

                return;
            }
            else
            {
                this.LogRetryTransaction();
            }
        }

        throw new CachingException( TooManyTransactionAttemptsErrorMessage );
    }

    public Task CleanUpAsync( CancellationToken cancellationToken = default )
    {
        var servers = this.Connection.GetEndPoints().Select( endpoint => this.Connection.GetServer( endpoint ) );

        return Task.WhenAll( servers.Select( s => this.CleanUpAsync( s, cancellationToken ) ) );
    }

    public Task CleanUpAsync( IServer server, CancellationToken cancellationToken = default )
    {
        List<Task> tasks = [];

        foreach ( var key in server.Keys( this.Database.Database, this.KeyBuilder.KeyPrefix + ":*", flags: this.Configuration.ReadCommandFlags ) )
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? keyString = key;

            if ( keyString == null )
            {
                continue;
            }

            var tokenizer = new StringTokenizer( keyString );

            var keyPrefix = tokenizer.GetNext( ':' );

            if ( !keyPrefix.Equals( this.KeyBuilder.KeyPrefix.AsSpan(), StringComparison.Ordinal ) )
            {
                this.LogSource.Warning.IfEnabled?.Write(
                    Formatted( "The key {Key} has an invalid prefix. Redis should not have returned it. Ignoring it.", keyPrefix.ToString() ) );

                continue;
            }

            var keyKind = tokenizer.GetNext( ':' ).ToString();

            if ( keyKind == RedisKeyBuilder.GarbageCollectionPrefix )
            {
                continue;
            }

            var smallKey = tokenizer.GetRemainder().ToString();

            switch ( keyKind )
            {
                case RedisKeyBuilder.ValueKindPrefix:
                    tasks.Add( this.CleanUpValue( key, smallKey ) );

                    break;

                case RedisKeyBuilder.DependenciesKindPrefix:
                    tasks.Add( this.CleanUpDependencies( smallKey ) );

                    break;

                case RedisKeyBuilder.DependencyKindPrefix:
                    tasks.Add( this.CleanUpDependency( smallKey ) );

                    break;
            }
        }

        return Task.WhenAll( tasks );
    }

    private async Task CleanUpDependency( string key )
    {
        var items = await this.Database.SetMembersAsync( this.KeyBuilder.GetDependencyKey( key ), this.Configuration.WriteCommandFlags );

        foreach ( var item in items )
        {
            string? itemString = item;

            if ( itemString == null )
            {
                continue;
            }

            if ( !await this.Database.KeyExistsAsync( this.KeyBuilder.GetValueKey( itemString ), this.Configuration.ReadCommandFlags ) )
            {
                this.LogSource.Warning.Write( Formatted( "The dependency key {Key} does not have the corresponding dependencies key. Deleting it.", key ) );

                await this.RemoveDependencyAsync( itemString, key, this.Database );
            }
        }
    }

    private async Task CleanUpDependencies( string key )
    {
        if ( !await this.Database.KeyExistsAsync( this.KeyBuilder.GetValueKey( key ), this.Configuration.ReadCommandFlags ) )
        {
            this.LogSource.Warning.Write( Formatted( "The dependencies key {Key} does not have the corresponding value key. Deleting it.", key ) );

            await this.DeleteItemAsync( key );
        }
    }

    private async Task CleanUpValue( RedisKey redisKey, string smallKey )
    {
        string? itemVersion = this.Database.ListGetByIndex( redisKey, _itemVersionIndex, this.Configuration.ReadCommandFlags );
        var dependencies = await this.GetDependenciesAsync( smallKey );

        if ( dependencies == null )
        {
            if ( itemVersion == _noDependencyVersion )
            {
                return;
            }

            this.LogSource.Warning.Write( Formatted( "The value key {Key} does not have the corresponding dependencies key. Deleting it.", smallKey ) );

            await this.Database.KeyDeleteAsync( redisKey, this.Configuration.WriteCommandFlags );

            return;
        }

        if ( itemVersion != dependencies[_itemVersionInDependenciesIndex] )
        {
            this.LogSource.Warning.Write(
                Formatted(
                    "The value {Key} version and the corresponding dependencies version differ. Deleting both.",
                    smallKey ) );

            for ( var i = _firstDependencyIndex; i < dependencies.Length; i++ )
            {
                await this.RemoveDependencyAsync( smallKey, dependencies[i], this.Database );
            }

            await this.Database.KeyDeleteAsync( this.KeyBuilder.GetDependenciesKey( smallKey ), this.Configuration.WriteCommandFlags );
            await this.Database.KeyDeleteAsync( redisKey, this.Configuration.WriteCommandFlags );

            return;
        }

        for ( var i = _firstDependencyIndex; i < dependencies.Length; i++ )
        {
            if ( !await this.Database.SetContainsAsync( this.KeyBuilder.GetDependencyKey( dependencies[i] ), smallKey, this.Configuration.WriteCommandFlags ) )
            {
                this.LogSource.Warning.Write(
                    Formatted(
                        "The value key {Key} does not have the corresponding dependency key {Dependency}. Deleting it.",
                        smallKey,
                        dependencies[i] ) );

                await this.DeleteItemAsync( smallKey );
            }
        }
    }

    private sealed class DependenciesRedisCachingBackendFeatures : RedisCachingBackendFeatures
    {
        public override bool Dependencies => true;

        public override bool ContainsDependency => true;
    }
}