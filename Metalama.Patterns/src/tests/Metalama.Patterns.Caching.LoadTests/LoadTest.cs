// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using System.Collections.Immutable;
using System.Numerics;

#pragma warning disable CA2201

namespace Metalama.Patterns.Caching.LoadTests;

internal sealed class LoadTest : IDisposable
{
    private sealed class TestClient : IDisposable
    {
        [Serializable]
        private class Payload
        {
            public string? Value { get; set; }

            public string[]? Dependencies { get; set; }
        }

        private const int _randomnessMultiplier = 1000;

        private readonly LoadTest _loadTest;
        private readonly CachingBackend _backend;
        private readonly Random _random = new();
        private readonly object _invalidatedDependenciesCountLock = new();
        private readonly object _removedItemsCountLock = new();
        private readonly StringCounter _errorsCounters = new();

        private Thread? _thread;
        private CancellationTokenSource? _cancellationTokenSource;

        private BigInteger _setItemCount = BigInteger.Zero;
        private BigInteger _cacheMissCount = BigInteger.Zero;
        private BigInteger _cacheHitCount = BigInteger.Zero;
        private BigInteger _invalidateDependencyCount = BigInteger.Zero;
        private BigInteger _removeItemCount = BigInteger.Zero;
        private BigInteger _invalidatedDependenciesCount = BigInteger.Zero;
        private BigInteger _evictedRemovedItemsCount = BigInteger.Zero;
        private BigInteger _expiredRemovedItemsCount = BigInteger.Zero;
        private BigInteger _invalidatedRemovedItemsCount = BigInteger.Zero;
        private BigInteger _removedItemsCount = BigInteger.Zero;
        private BigInteger _otherRemovedItemsCount = BigInteger.Zero;
        private BigInteger _unknownRemovedItemsCount = BigInteger.Zero;
        private BigInteger _sharedDependencies = BigInteger.Zero;

        public TestClient( LoadTest loadTest, Func<CachingBackend> cachingBackendFactory )
        {
            this._loadTest = loadTest;
            this._backend = cachingBackendFactory.Invoke();

            this._backend.DependencyInvalidated += ( _, _ ) =>
            {
                lock ( this._invalidatedDependenciesCountLock )
                {
                    this._invalidatedDependenciesCount++;
                }
            };

            this._backend.ItemRemoved += ( _, args ) =>
            {
                lock ( this._removedItemsCountLock )
                {
                    switch ( args.RemovedReason )
                    {
                        case CacheItemRemovedReason.Evicted:
                            this._evictedRemovedItemsCount++;

                            break;

                        case CacheItemRemovedReason.Expired:
                            this._expiredRemovedItemsCount++;

                            break;

                        case CacheItemRemovedReason.Invalidated:
                            this._invalidatedRemovedItemsCount++;

                            break;

                        case CacheItemRemovedReason.Removed:
                            this._removedItemsCount++;

                            break;

                        case CacheItemRemovedReason.Other:
                            this._otherRemovedItemsCount++;

                            break;

                        default:
                            this._unknownRemovedItemsCount++;

                            break;
                    }
                }
            };
        }

        public void Start()
        {
            if ( this._thread != null )
            {
                throw new InvalidOperationException( "The client is already running." );
            }

            this._cancellationTokenSource = new CancellationTokenSource();
            this._thread = new Thread( this.Run );
            this._thread.Start();
        }

        private void Run()
        {
            while ( !this._cancellationTokenSource!.IsCancellationRequested )
            {
                var nextAction = this.GetNextRandomNumber( 4 );

                switch ( nextAction )
                {
                    case 0:
                        this.SetItem();

                        break;

                    case 1:
                        this.GetItem();

                        break;

                    case 2:
                        this.RemoveItem();

                        break;

                    case 3:
                        this.InvalidateDependency();

                        break;

                    default:
                        throw new Exception( "Unknown action " + nextAction );
                }
            }
        }

        private int GetNextRandomNumber( int maxValue )
        {
            return this.GetNextRandomNumber( 0, maxValue );
        }

        private int GetNextRandomNumber( Interval interval )
        {
            return this.GetNextRandomNumber( interval.Min, interval.Max + 1 );
        }

        private int GetNextRandomNumber( int minValue, int maxValue )
        {
            var range = maxValue - minValue;

            return (this._random.Next( 0, range * _randomnessMultiplier ) % range) + minValue;
        }

        private string GetRandomValueKey()
        {
            var index = this.GetNextRandomNumber( this._loadTest._valueKeys.Length );

            return this._loadTest._valueKeys[index];
        }

        private string GetRandomDependencyKey()
        {
            var index = this.GetNextRandomNumber( this._loadTest._dependencyKeys.Length );

            return this._loadTest._dependencyKeys[index];
        }

        private void SetItem( string? sharedDependency = null )
        {
            var key = this.GetRandomValueKey();

            var payload = new Payload { Value = new string( 'a', this.GetNextRandomNumber( this._loadTest._configuration.ValueLength ) ) };

            var dependenciesCount = this.GetNextRandomNumber( this._loadTest._configuration.DependenciesPerValueCount );

            if ( sharedDependency != null )
            {
                dependenciesCount++;
            }

            CacheItem item;

            var configuration = new CacheItemConfiguration()
            {
                SlidingExpiration =
                    TimeSpan.FromSeconds( this.GetNextRandomNumber( this._loadTest._configuration.ValueKeyExpiry ) )
            };

            string? dependencyToShare = null;

            if ( dependenciesCount == 0 )
            {
                item = new CacheItem( payload, configuration: configuration );
            }
            else
            {
                var dependencies = new string[dependenciesCount];

                for ( var i = sharedDependency == null ? 0 : 1; i < dependencies.Length; i++ )
                {
                    dependencies[i] = this.GetRandomDependencyKey();
                }

                if ( sharedDependency == null )
                {
                    dependencyToShare = dependencies[0];
                }
                else
                {
                    dependencies[0] = sharedDependency;
                }

                payload.Dependencies = dependencies;

                item = new CacheItem( payload, ImmutableArray.Create( dependencies ), configuration );
            }

            this._backend.SetItem( key, item );
            this._setItemCount++;

            if ( dependencyToShare != null )
            {
                this._sharedDependencies++;

                var valuesSharingDependency = this.GetNextRandomNumber( this._loadTest._configuration.ValuesPerSharedDependency );

                for ( var i = 0; i < valuesSharingDependency; i++ )
                {
                    this.SetItem( dependencyToShare );
                }
            }
        }

        private void GetItem()
        {
            var key = this.GetRandomValueKey();
            var value = this._backend.GetItem( key );

            if ( value == null )
            {
                this._cacheMissCount++;

                return;
            }

            this._cacheHitCount++;

            if ( value.Value is not Payload payload )
            {
                this._errorsCounters.Increment(
                    "Corrupted payload type.",
                    value.Value == null
                        ? $"payload is null, dependencies: {string.Join( ", ", value.Dependencies.EmptyIfDefault() )}"
                        : value.Value.GetType().FullName + ", " + value.Value );

                return;
            }

            var retrievedDependenciesList = value.Dependencies;

            if ( payload.Dependencies == null && retrievedDependenciesList.IsDefaultOrEmpty )
            {
                // No dependencies as expected
                return;
            }

            if ( payload.Dependencies is { Length: 0 } )
            {
                this._errorsCounters.Increment( "Corrupted payload - dependencies missing." );
            }

            if ( payload.Dependencies == null && retrievedDependenciesList.Length > 0 )
            {
                this._errorsCounters.Increment(
                    "Corrupted dependencies - dependencies not expected but retrieved.",
                    $"Retrieved: {string.Join( " ", retrievedDependenciesList )}" );

                return;
            }

            if ( payload.Dependencies != null && payload.Dependencies.Length != retrievedDependenciesList.Length )
            {
                this._errorsCounters.Increment(
                    "Corrupted dependencies - different number of expected and retrieved dependencies.",
                    $"Expected: {string.Join( " ", retrievedDependenciesList )} Retrieved: {string.Join( " ", payload.Dependencies )}" );

                return;
            }

            HashSet<string> retrievedDependencies = new( retrievedDependenciesList );

            foreach ( var expectedDependency in payload.Dependencies! )
            {
                if ( !retrievedDependencies.Contains( expectedDependency ) )
                {
                    this._errorsCounters.Increment(
                        "Expected dependency not present.",
                        $"Expected: {string.Join( " ", retrievedDependenciesList )} Retrieved: {string.Join( " ", payload.Dependencies )}" );

                    return;
                }
            }
        }

        private void RemoveItem()
        {
            var key = this.GetRandomValueKey();
            this._backend.RemoveItem( key );
            this._removeItemCount++;
        }

        private void InvalidateDependency()
        {
            var dependency = this.GetRandomDependencyKey();
            this._backend.InvalidateDependency( dependency );
            this._invalidateDependencyCount++;
        }

        public void Stop()
        {
            this._cancellationTokenSource!.Cancel();
            this._thread!.Join();
            this._thread = null;
        }

        public void Report()
        {
            Console.WriteLine( "Operations:" );
            Console.WriteLine( $"setItemCount: {this._setItemCount}" );
            Console.WriteLine( $"sharedDependenciesCount: {this._sharedDependencies}" );
            Console.WriteLine( $"cacheMissCount: {this._cacheMissCount}" );
            Console.WriteLine( $"cacheHitCount: {this._cacheHitCount}" );
            Console.WriteLine( $"invalidateDependencyCount: {this._invalidateDependencyCount}" );
            Console.WriteLine( $"removeItemCount: {this._removeItemCount}" );
            Console.WriteLine( "Events:" );
            Console.WriteLine( $"invalidatedDependenciesCount: {this._invalidatedDependenciesCount}" );
            Console.WriteLine( $"evictedRemovedItemsCount: {this._evictedRemovedItemsCount}" );
            Console.WriteLine( $"expiredRemovedItemsCount: {this._expiredRemovedItemsCount}" );
            Console.WriteLine( $"invalidatedRemovedItemsCount: {this._invalidatedRemovedItemsCount}" );
            Console.WriteLine( $"removedItemsCount: {this._removedItemsCount}" );
            Console.WriteLine( $"otherRemovedItemsCount: {this._otherRemovedItemsCount}" );
            Console.WriteLine( $"unknownRemovedItemsCount: {this._unknownRemovedItemsCount}" );
            Console.WriteLine( "Errors:" );

            if ( this._errorsCounters.Counters.Count > 0 )
            {
                foreach ( var error in this._errorsCounters.Counters )
                {
                    Console.WriteLine( $"> {error.Key}: {error.Value}" );

                    if ( this._errorsCounters.Details.TryGetValue( error.Key, out var details ) )
                    {
                        foreach ( var detail in details )
                        {
                            Console.WriteLine( detail );
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine( "No error." );
            }
        }

        public void Dispose()
        {
            this._backend.Dispose();
        }
    }

    private readonly LoadTestConfiguration _configuration;

    // "Guaranteed" to be initialized by Initialize(...)
    private string[] _valueKeys = null!;

    // "Guaranteed" to be initialized by Initialize(...)
    private string[] _dependencyKeys = null!;

    // "Guaranteed" to be initialized by Initialize(...)
    private TestClient[] _clients = null!;

    public LoadTest( LoadTestConfiguration configuration )
    {
        this._configuration = configuration;
    }

    public void Initialize( Func<CachingBackend> backendFactory )
    {
        this._valueKeys = GenerateKeys( this._configuration.ValueKeysCount, this._configuration.ValueKeyLength );
        this._dependencyKeys = GenerateKeys( this._configuration.DependencyKeysCount, this._configuration.DependencyKeyLength );
        this._clients = new TestClient[this._configuration.ClientsCount];

        for ( var i = 0; i < this._configuration.ClientsCount; i++ )
        {
            this._clients[i] = new TestClient( this, backendFactory );
        }
    }

    private static string[] GenerateKeys( int count, Interval length )
    {
        var keys = new string[count];

        for ( var i = 0; i < count; i++ )
        {
            keys[i] = RandomString.New( length.Min, length.Max );
        }

        return keys;
    }

    public void Start()
    {
        var startTasks = new Task[this._configuration.ClientsCount];

        for ( var i = 0; i < this._configuration.ClientsCount; i++ )
        {
            var client = this._clients[i];
            startTasks[i] = Task.Run( () => client.Start() );
        }

        Task.WaitAll( startTasks );
    }

    public void Stop()
    {
        var stopTasks = new Task[this._configuration.ClientsCount];

        for ( var i = 0; i < this._configuration.ClientsCount; i++ )
        {
            var client = this._clients[i];
            stopTasks[i] = Task.Run( () => client.Stop() );
        }

        Task.WaitAll( stopTasks );
    }

    public void Report()
    {
        for ( var i = 0; i < this._clients.Length; i++ )
        {
            Console.WriteLine( $"Client {i + 1}" );
            this._clients[i].Report();
            Console.WriteLine();
        }
    }

    public void Dispose()
    {
        var disposeTasks = new Task[this._configuration.ClientsCount];

        for ( var i = 0; i < this._configuration.ClientsCount; i++ )
        {
            var client = this._clients[i];
            disposeTasks[i] = Task.Run( () => client.Dispose() );
        }

        Task.WaitAll( disposeTasks );
    }
}