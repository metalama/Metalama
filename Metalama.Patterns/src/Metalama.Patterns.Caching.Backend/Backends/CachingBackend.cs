// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace;
using JetBrains.Annotations;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using static Flashtrace.Messages.FormattedMessageBuilder;

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// An abstraction of the physical implementation of the cache.
/// </summary>
[PublicAPI]
public abstract class CachingBackend : IDisposable, IAsyncDisposable
{
    private static readonly ValueTask<bool> _falseTaskResult = new( false );
    private static readonly ValueTask<bool> _trueTaskResult = new( true );
    private static readonly ValueTask _completedTask = new( Task.CompletedTask );
    private readonly ICachingExceptionObserver? _exceptionObserver;
    private readonly TaskCompletionSource<bool> _disposeTask = new();
    private readonly SemaphoreSlim _initializeSemaphore = new( 1, 1 );
    private readonly CancellationTokenSource _disposeCancellationTokenSource = new();

    private CachingBackendFeatures? _features;
    private int _status;
    private EventHandler<CacheItemRemovedEventArgs>? _itemRemoved;
    private EventHandler<CacheDependencyInvalidatedEventArgs>? _dependencyInvalidated;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingBackend"/> class.
    /// </summary>
    protected CachingBackend( CachingBackendConfiguration? configuration = null, IServiceProvider? serviceProvider = null )
    {
        this.ServiceProvider = serviceProvider ?? NullServiceProvider.Instance;
        this._exceptionObserver = serviceProvider?.GetService<ICachingExceptionObserver>();
        this.Configuration = configuration ?? new MemoryCachingBackendConfiguration();
        this.LogSource = serviceProvider.GetFlashtraceSource( this.GetType(), FlashtraceRole.Caching );
        this.DebugName = this.Configuration.DebugName ?? this.Id.ToString();
    }

    public IServiceProvider ServiceProvider { get; }

    protected CachingBackendConfiguration Configuration { get; }

    /// <summary>
    /// Gets a <see cref="CancellationToken"/> signalled when the <see cref="CancellationToken"/> passed to <see cref="Dispose()"/> or <see cref="DisposeAsync()"/>
    /// is signalled.
    /// </summary>
    protected CancellationToken DisposeCancellationToken { get; }

    public string? DebugName { get; }

    // ReSharper disable once MemberInitializerValueIgnored 

    /// <summary>
    /// Gets the <see cref="FlashtraceSource"/> that implementations can use to emit
    /// log records.
    /// </summary>
    protected FlashtraceSource LogSource { get; } = FlashtraceSource.Null; /* Make sure we have a default value in case of exception in the constructor. */

    /// <summary>
    /// Gets the <see cref="Guid"/> of the current <see cref="CachingBackend"/>.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Creates a <see cref="CachingBackendFeatures"/> object, which describes set of features implemented by the back-end.
    /// This method is invoked the first time the <see cref="SupportedFeatures"/> property is evaluated. The result is then cached.
    /// </summary>
    /// <returns>A new instance of the <see cref="CachingBackendEnhancerFeatures"/> class.</returns>
    protected virtual CachingBackendFeatures CreateFeatures() => new();

    /// <summary>
    /// Gets the set of features supported by the current <see cref="CachingBackend"/>.
    /// </summary>
    public CachingBackendFeatures SupportedFeatures => this._features ??= this.CreateFeatures();

    public void Initialize()
    {
        var status = this.Status;

        if ( status is CachingBackendStatus.Failed )
        {
            throw new InvalidOperationException( "A previous initialization has failed." );
        }
        else if ( status is not (CachingBackendStatus.Default or CachingBackendStatus.Initializing) )
        {
            // The component is already initialized.
            return;
        }

        // ReSharper disable once MethodSupportsCancellation
        this._initializeSemaphore.Wait();

        try
        {
            if ( !this.TryChangeStatus( CachingBackendStatus.Default, CachingBackendStatus.Initializing ) )
            {
                return;
            }

            this.InitializeCore();

            if ( !this.TryChangeStatus( CachingBackendStatus.Initializing, CachingBackendStatus.Initialized ) )
            {
                throw new CachingAssertionFailedException();
            }
        }
        catch
        {
            this.TryChangeStatus( CachingBackendStatus.Initializing, CachingBackendStatus.Failed );

            throw;
        }
        finally
        {
            this._initializeSemaphore.Release();
        }
    }

    public async ValueTask InitializeAsync( CancellationToken cancellationToken = default )
    {
        if ( this.Status is CachingBackendStatus.Failed )
        {
            throw new InvalidOperationException( "A previous initialization has failed." );
        }
        else if ( this.Status is not (CachingBackendStatus.Default or CachingBackendStatus.Initializing) )
        {
            // The component is already initialized.
            return;
        }

        await this._initializeSemaphore.WaitAsync( cancellationToken );

        try
        {
            if ( !this.TryChangeStatus( CachingBackendStatus.Default, CachingBackendStatus.Initializing ) )
            {
                return;
            }

            await this.InitializeCoreAsync( cancellationToken );

            if ( !this.TryChangeStatus( CachingBackendStatus.Initializing, CachingBackendStatus.Initialized ) )
            {
                throw new CachingAssertionFailedException();
            }
        }
        catch
        {
            this.TryChangeStatus( CachingBackendStatus.Initializing, CachingBackendStatus.Failed );

            throw;
        }
        finally
        {
            this._initializeSemaphore.Release();
        }
    }

    protected virtual void InitializeCore() { }

    protected virtual Task InitializeCoreAsync( CancellationToken cancellationToken = default ) => Task.CompletedTask;

    private void CheckStatus()
    {
        this.Initialize();

        if ( this.Status != CachingBackendStatus.Initialized )
        {
            throw new ObjectDisposedException( this.ToString() );
        }
    }

    private async ValueTask CheckStatusAsync( CancellationToken cancellationToken )
    {
        await this.InitializeAsync( cancellationToken );

        if ( this.Status != CachingBackendStatus.Initialized )
        {
            throw new ObjectDisposedException( this.ToString() );
        }
    }

    private void RequireFeature( bool feature, string featureName )
    {
        if ( !feature )
        {
            throw new NotSupportedException(
                string.Format( CultureInfo.InvariantCulture, "Feature \"{0}\" is not supported by {1}.", featureName, this.GetType().Name ) );
        }
    }

    /// <summary>
    /// Sets a cache item. This protected method is part of the implementation API and is meant to be overridden in user code, not invoked. Arguments are already validated by the consumer API.
    /// </summary>
    /// <param name="key">The key of the cache item.</param>
    /// <param name="item">The cache item.</param>
    protected abstract void SetItemCore( string key, CacheItem item );

    /// <summary>
    /// Asynchronously sets a cache item. This protected method is part of the implementation API and is meant to be overridden in user code, not invoked. Arguments are already validated by the consumer API.
    /// The default implementation is to call the synchronous <see cref="SetItemCore(string, CacheItem)"/> method.
    /// </summary>
    /// <param name="key">The key of the cache item.</param>
    /// <param name="item">The cache item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    protected virtual ValueTask SetItemAsyncCore( string key, CacheItem item, CancellationToken cancellationToken )
    {
        cancellationToken.ThrowIfCancellationRequested();

        this.SetItemCore( key, item );

        return new ValueTask( Task.CompletedTask );
    }

    /// <summary>
    /// Sets a cache item.
    /// </summary>
    /// <param name="key">The key of the cache item.</param>
    /// <param name="item">The cache item.</param>
    public void SetItem( string key, CacheItem item )
    {
        using ( var activity = this.LogSource.Default.OpenActivity( Formatted( """SetItem( backend = "{Backend}" key = "{Key}" )""", this, key ) ) )
        {
            try
            {
                this.CheckStatus();
                this.Validate( item );

                this.SetItemCore( key, item );

                activity.SetSuccess();
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Asynchronously sets a cache item.
    /// </summary>
    /// <param name="key">The key of the cache item.</param>
    /// <param name="item">The cache item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public async ValueTask SetItemAsync( string key, CacheItem item, CancellationToken cancellationToken = default )
    {
        using ( var activity = this.LogSource.Default.OpenAsyncActivity( Formatted( """SetItemAsync( backend = "{Backend}" key = "{Key}" )""", this, key ) ) )
        {
            try
            {
                await this.CheckStatusAsync( cancellationToken );
                this.Validate( item );

                cancellationToken.ThrowIfCancellationRequested();

                await this.SetItemAsyncCore( key, item, cancellationToken );

                activity.SetSuccess();
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Determines whether the cache contains an item of a given key. This protected method is part of the implementation API and is meant to be overridden in user code, not invoked. Arguments are already validated by the consumer API.
    /// </summary>
    /// <param name="key">The key of the cache item.</param>
    /// <returns><c>true</c> if the cache contains an item whose key is <paramref name="key"/>, otherwise <c>false</c>.</returns>
    protected abstract bool ContainsItemCore( string key );

    /// <summary>
    /// Asynchronously determines whether the cache contains an item of a given key. This protected method is part of the implementation API and is meant to be overridden in user code, not invoked. Arguments are already validated by the consumer API.
    /// </summary>
    /// <param name="key">The key of the cache item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> that evaluates to <c>true</c> if the cache contains an item whose key is <paramref name="key"/>, otherwise <c>false</c>.</returns>
    protected virtual ValueTask<bool> ContainsItemAsyncCore( string key, CancellationToken cancellationToken )
    {
        cancellationToken.ThrowIfCancellationRequested();

        return this.ContainsItemCore( key ) ? _trueTaskResult : _falseTaskResult;
    }

    /// <summary>
    /// Determines whether the cache contains an item of a given key.
    /// </summary>
    /// <param name="key">The key of the cache item.</param>
    /// <returns><c>true</c> if the cache contains an item whose key is <paramref name="key"/>, otherwise <c>false</c>.</returns>
    public bool ContainsItem( string key )
    {
        using ( var activity = this.LogSource.Default.OpenActivity( Formatted( "ContainsItem( \"{Key}\" )", key ) ) )
        {
            try
            {
                this.CheckStatus();
                var present = this.ContainsItemCore( key );

                activity.SetResult( present ? "Cache hit." : "Cache miss." );

                return present;
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Asynchronously determines whether the cache contains an item of a given key.
    /// </summary>
    /// <param name="key">The key of the cache item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> that evaluates to <c>true</c> if the cache contains an item whose key is <paramref name="key"/>, otherwise <c>false</c>.</returns>
    public async ValueTask<bool> ContainsItemAsync( string key, CancellationToken cancellationToken = default )
    {
        using ( var activity = this.LogSource.Default.OpenAsyncActivity( Formatted( "ContainsItemAsync( \"{Key}\" )", key ) ) )
        {
            try
            {
                await this.CheckStatusAsync( cancellationToken );
                cancellationToken.ThrowIfCancellationRequested();

                var present = await this.ContainsItemAsyncCore( key, cancellationToken );

                activity.SetResult( present ? "Cache hit." : "Cache miss." );

                return present;
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Gets a cache item given its key. This protected method is part of the implementation API and is meant to be overridden in user code, not invoked. Arguments are already validated by the consumer API.
    /// </summary>
    /// <param name="key">The cache item.</param>
    /// <param name="includeDependencies"><c>true</c> if the <see cref="CacheItem.Dependencies"/> properties of the
    /// resulting <see cref="CacheItem"/> should be populated, otherwise <c>false</c>.</param>
    /// <returns>A <see cref="CacheItem"/>, or <c>null</c> if there is no item in cache of the given <paramref name="key"/>.</returns>
    protected abstract CacheItem? GetItemCore( string key, bool includeDependencies );

    /// <summary>
    /// Asynchronously gets a cache item given its key. This protected method is part of the implementation API and is meant to be overridden in user code, not invoked. Arguments are already validated by the consumer API.
    /// </summary>
    /// <param name="key">The cache item.</param>
    /// <param name="includeDependencies"><c>true</c> if the <see cref="CacheItem.Dependencies"/> properties of the
    ///     resulting <see cref="CacheItem"/> should be populated, otherwise <c>false</c>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> evaluating to a <see cref="CacheItem"/>, or evaluating to <c>null</c> if there is no item in cache of the given <paramref name="key"/>.</returns>
    protected virtual ValueTask<CacheItem?> GetItemAsyncCore( string key, bool includeDependencies, CancellationToken cancellationToken )
    {
        cancellationToken.ThrowIfCancellationRequested();

        return new ValueTask<CacheItem?>( this.GetItemCore( key, includeDependencies ) );
    }

    /// <summary>
    /// Gets a cache item given its key.
    /// </summary>
    /// <param name="key">The cache item.</param>
    /// <param name="includeDependencies"><c>true</c> if the <see cref="CacheItem.Dependencies"/> properties of the
    /// resulting <see cref="CacheItem"/> should be populated, otherwise <c>false</c>.</param>
    /// <returns>A <see cref="CacheItem"/>, or <c>null</c> if there is no item in cache of the given <paramref name="key"/>.</returns>
    public CacheItem? GetItem( string key, bool includeDependencies = true )
    {
        using ( var activity = this.LogSource.Default.OpenActivity( Formatted( """GetItem( backend = "{Backend}" key = "{Key}" )""", this, key ) ) )
        {
            try
            {
                this.CheckStatus();

                CacheItem? item = null;

                try
                {
                    item = this.GetItemCore( key, includeDependencies );
                }
                catch ( Exception e ) when ( e is InvalidCacheItemException or InvalidCastException )
                {
                    if ( this._exceptionObserver.OnException( e, false ) )
                    {
                        throw;
                    }

                    this.LogSource.Warning.Write(
                        Formatted(
                            "The cached object or method source code or caching settings may have changed since the value has been cached. Removing the item. "
                            +
                            "To avoid this warning, change the cache key prefix when you do breaking changes to the source code." ),
                        e );

                    this.RemoveItemCore( key );
                }

                activity.SetResult( item != null ? "Cache hit." : "Cache miss." );

                return item;
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Asynchronously gets a cache item given its key.
    /// </summary>
    /// <param name="key">The cache item.</param>
    /// <param name="includeDependencies"><c>true</c> if the <see cref="CacheItem.Dependencies"/> properties of the
    /// resulting <see cref="CacheItem"/> should be populated, otherwise <c>false</c>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> evaluating to a <see cref="CacheItem"/>, or evaluating to <c>null</c> if there is no item in cache of the given <paramref name="key"/>.</returns>
    public async ValueTask<CacheItem?> GetItemAsync(
        string key,
        bool includeDependencies = true,
        CancellationToken cancellationToken = default )
    {
        using ( var activity = this.LogSource.Default.OpenAsyncActivity( Formatted( """GetItemAsync( backend = "{Backend}" key = "{Key}" )""", this, key ) ) )
        {
            try
            {
                await this.CheckStatusAsync( cancellationToken );
                cancellationToken.ThrowIfCancellationRequested();

                CacheItem? item = null;

                try
                {
                    item = await this.GetItemAsyncCore( key, includeDependencies, cancellationToken );
                }
                catch ( Exception e ) when ( e is InvalidCacheItemException or InvalidCastException )
                {
                    if ( this._exceptionObserver.OnException( e, false ) )
                    {
                        throw;
                    }

                    this.LogSource.Warning.Write(
                        Formatted(
                            "The cached object or method source code or caching settings may have changed since the value has been cached. Removing the item." +
                            "To avoid this warning, change the cache key prefix when you do breaking changes to the source code." ),
                        e );

                    await this.RemoveItemAsyncCore( key, cancellationToken );
                }

                activity.SetResult( item != null ? "Cache hit." : "Cache miss." );

                return item;
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Removes a cache item from the cache given its key. This protected method is part of the implementation API and is meant to be overridden in user code, not invoked. Arguments are already validated by the consumer API.
    /// </summary>
    /// <param name="key">The key of the cache item.</param>
    protected abstract void RemoveItemCore( string key );

    /// <summary>
    /// Asynchronously removes a cache item from the cache given its key. This protected method is part of the implementation API and is meant to be overridden in user code, not invoked. Arguments are already validated by the consumer API.
    /// </summary>
    /// <param name="key">The key of the cache item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    protected virtual ValueTask RemoveItemAsyncCore( string key, CancellationToken cancellationToken )
    {
        cancellationToken.ThrowIfCancellationRequested();

        this.RemoveItemCore( key );

        return _completedTask;
    }

    /// <summary>
    /// Removes a cache item from the cache given its key.
    /// </summary>
    /// <param name="key">The key of the cache item.</param>
    public void RemoveItem( string key )
    {
        using ( var activity = this.LogSource.Default.OpenActivity( Formatted( "RemoveItem( \"{Key}\" )", key ) ) )
        {
            try
            {
                this.CheckStatus();
                this.RemoveItemCore( key );

                activity.SetSuccess();
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Asynchronously removes a cache item from the cache given its key.
    /// </summary>
    /// <param name="key">The key of the cache item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public async ValueTask RemoveItemAsync( string key, CancellationToken cancellationToken = default )
    {
        using ( var activity = this.LogSource.Default.OpenAsyncActivity( Formatted( "RemoveItemAsync( \"{Key}\" )", key ) ) )
        {
            try
            {
                await this.CheckStatusAsync( cancellationToken );
                cancellationToken.ThrowIfCancellationRequested();

                await this.RemoveItemAsyncCore( key, cancellationToken );

                activity.SetSuccess();
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Removes from the cache all items that have a specific dependency.  This protected method is part of the implementation API and is meant to be overridden in user code, not invoked. Arguments are already validated by the consumer API.
    /// </summary>
    /// <param name="key">The dependency key.</param>
    protected abstract void InvalidateDependencyCore( string key );

    /// <summary>
    /// Asynchronously removes from the cache all items that have a specific dependency.
    /// This protected method is part of the implementation API and is meant to be overridden in user code, not invoked. Arguments are already validated by the consumer API.
    /// </summary>
    /// <param name="key">The dependency key.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    protected virtual ValueTask InvalidateDependencyAsyncCore( string key, CancellationToken cancellationToken )
    {
        cancellationToken.ThrowIfCancellationRequested();

        this.InvalidateDependencyCore( key );

        return _completedTask;
    }

    /// <summary>
    /// Removes from the cache all items that have a specific dependency.
    /// </summary>
    /// <param name="key">The dependency key.</param>
    public void InvalidateDependency( string key )
    {
        using ( var activity = this.LogSource.Default.OpenActivity( Formatted( "InvalidateDependency( \"{Key}\" )", key ) ) )
        {
            try
            {
                this.CheckStatus();
                this.RequireFeature( this.SupportedFeatures.Dependencies, nameof(this.SupportedFeatures.Dependencies) );

                this.InvalidateDependencyCore( key );

                activity.SetSuccess();
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Removes from the cache all items that have a specific dependency.
    /// </summary>
    public void InvalidateDependencies( IReadOnlyCollection<string> keys )
    {
        using ( var activity = this.LogSource.Default.OpenActivity( Formatted( "InvalidateDependencies( \"{Keys}\" )", keys ) ) )
        {
            try
            {
                this.CheckStatus();
                this.RequireFeature( this.SupportedFeatures.Dependencies, nameof(this.SupportedFeatures.Dependencies) );

                foreach ( var key in keys )
                {
                    this.InvalidateDependencyCore( key );
                }

                activity.SetSuccess();
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Asynchronously removes from the cache all items that have a specific dependency. 
    /// </summary>
    /// <param name="key">The dependency key.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public async ValueTask InvalidateDependencyAsync( string key, CancellationToken cancellationToken = default )
    {
        using ( var activity =
               this.LogSource.Default.OpenAsyncActivity( Formatted( "InvalidateDependencyAsync( Backend=\"{Backend}\", Key=\"{Key}\" )", this, key ) ) )
        {
            try
            {
                await this.CheckStatusAsync( cancellationToken );
                this.RequireFeature( this.SupportedFeatures.Dependencies, nameof(this.SupportedFeatures.Dependencies) );

                cancellationToken.ThrowIfCancellationRequested();

                await this.InvalidateDependencyAsyncCore( key, cancellationToken );

                activity.SetSuccess();
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Asynchronously removes from the cache all items that have one of the specified dependencies.
    /// </summary>
    /// <param name="keys">The dependency keys.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public async ValueTask InvalidateDependenciesAsync( IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default )
    {
        using ( var activity =
               this.LogSource.Default.OpenAsyncActivity( Formatted( "InvalidateDependenciesAsync( Backend=\"{Backend}\", Keys=\"{Keys}\" )", this, keys ) ) )
        {
            try
            {
                await this.CheckStatusAsync( cancellationToken );
                this.RequireFeature( this.SupportedFeatures.Dependencies, nameof(this.SupportedFeatures.Dependencies) );

                cancellationToken.ThrowIfCancellationRequested();

                List<Task>? tasks = null;

                foreach ( var key in keys )
                {
                    var task = this.InvalidateDependencyAsyncCore( key, cancellationToken );

                    if ( !task.IsCompleted )
                    {
                        tasks ??= new List<Task>( keys.Count );
                        tasks.Add( task.AsTask() );
                    }
                }

                if ( tasks != null )
                {
                    await Task.WhenAll( tasks );
                }

                activity.SetSuccess();
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Determines whether the cache contains a given dependency.  This protected method is part of the implementation API and is meant to be overridden in user code, not invoked. Arguments are already validated by the consumer API.
    /// </summary>
    /// <param name="key">The dependency key.</param>
    /// <returns><c>true</c> if the cache contains the dependency <paramref name="key"/>, otherwise <c>false</c>.</returns>
    protected abstract bool ContainsDependencyCore( string key );

    /// <summary>
    /// Asynchronously determines whether the cache contains a given dependency.  This protected method is part of the implementation API and is meant to be overridden in user code, not invoked. Arguments are already validated by the consumer API.
    /// </summary>
    /// <param name="key">The dependency key.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> evaluating to <c>true</c> if the cache contains the dependency <paramref name="key"/>, otherwise <c>false</c>.</returns>
    protected virtual ValueTask<bool> ContainsDependencyAsyncCore( string key, CancellationToken cancellationToken )
    {
        cancellationToken.ThrowIfCancellationRequested();

        return this.ContainsDependencyCore( key ) ? _trueTaskResult : _falseTaskResult;
    }

    /// <summary>
    /// Determines whether the cache contains a given dependency.
    /// </summary>
    /// <param name="key">The dependency key.</param>
    /// <returns><c>true</c> if the cache contains the dependency <paramref name="key"/>, otherwise <c>false</c>.</returns>
    public bool ContainsDependency( string key )
    {
        using ( var activity = this.LogSource.Default.OpenActivity( Formatted( "ContainsDependency( \"{Key}\" )", key ) ) )
        {
            try
            {
                this.CheckStatus();
                this.RequireFeature( this.SupportedFeatures.Dependencies, nameof(this.SupportedFeatures.Dependencies) );
                this.RequireFeature( this.SupportedFeatures.ContainsDependency, nameof(this.SupportedFeatures.ContainsDependency) );

                var present = this.ContainsDependencyCore( key );

                activity.SetResult( present ? "Cache hit." : "Cache miss." );

                return present;
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Asynchronously determines whether the cache contains a given dependency.
    /// </summary>
    /// <param name="key">The dependency key.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> evaluating to <c>true</c> if the cache contains the dependency <paramref name="key"/>, otherwise <c>false</c>.</returns>
    public async ValueTask<bool> ContainsDependencyAsync( string key, CancellationToken cancellationToken = default )
    {
        using ( var activity = this.LogSource.Default.OpenAsyncActivity( Formatted( "ContainsDependencyAsync( \"{Key}\" )", key ) ) )
        {
            try
            {
                await this.CheckStatusAsync( cancellationToken );

                this.RequireFeature( this.SupportedFeatures.Dependencies, nameof(this.SupportedFeatures.Dependencies) );
                this.RequireFeature( this.SupportedFeatures.ContainsDependency, nameof(this.SupportedFeatures.ContainsDependency) );

                cancellationToken.ThrowIfCancellationRequested();

                var present = await this.ContainsDependencyAsyncCore( key, cancellationToken );

                activity.SetResult( present ? "Cache hit." : "Cache miss." );

                return present;
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Clears the cache. This protected method is part of the implementation API and is meant to be overridden in user code, not invoked. 
    /// </summary>
    /// <param name="options"></param>
    protected abstract void ClearCore( ClearCacheOptions options );

    /// <summary>
    /// Asynchronously clears the cache. This protected method is part of the implementation API and is meant to be overridden in user code, not invoked.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    protected virtual ValueTask ClearAsyncCore( ClearCacheOptions options, CancellationToken cancellationToken )
    {
        this.ClearCore( options );

        return _completedTask;
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear( ClearCacheOptions options = default )
    {
        using ( var activity = this.LogSource.Default.OpenActivity( Formatted( "Clear()" ) ) )
        {
            try
            {
                this.CheckStatus();
                this.ClearCore( options );

                activity.SetSuccess();
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Asynchronously clears the cache.
    /// </summary>
    /// <param name="options">Options.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public async ValueTask ClearAsync( ClearCacheOptions options = ClearCacheOptions.Default, CancellationToken cancellationToken = default )
    {
        using ( var activity = this.LogSource.Default.OpenAsyncActivity( Formatted( "ClearAsync()" ) ) )
        {
            try
            {
                await this.CheckStatusAsync( cancellationToken );
                cancellationToken.ThrowIfCancellationRequested();

                await this.ClearAsyncCore( options, cancellationToken );
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }
    }

    /// <summary>
    /// Gets the status of the current <see cref="CachingBackend"/> (<see cref="CachingBackendStatus.Default"/>,
    /// <see cref="CachingBackendStatus.Disposing"/> or <see cref="CachingBackendStatus.Disposed"/>).
    /// </summary>
    public CachingBackendStatus Status => (CachingBackendStatus) this._status;

    private bool TryChangeStatus( CachingBackendStatus expectedStatus, CachingBackendStatus newStatus )
        => Interlocked.CompareExchange( ref this._status, (int) newStatus, (int) expectedStatus ) == (int) expectedStatus;

    /// <inheritdoc />
    public override string ToString()
        => string.Format( CultureInfo.InvariantCulture, "{{{0} Id={1}, Status={2}}}", this.GetType().Name, this.DebugName, this.Status );

    public ValueTask DisposeAsync() => this.DisposeAsync( default );

    /// <summary>
    /// Returns a <see cref="Task"/> that is signaled to the complete state when all background tasks
    /// have completed. 
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> that is signaled to the complete state when all background tasks
    /// have completed.</returns>
    /// <remarks>
    /// <para>
    /// Other background tasks may possibly be enqueued between the time
    /// the returned <see cref="Task"/> has been signaled and the time the signal is processed
    /// by the caller.
    /// </para>
    /// </remarks>
    public virtual Task WhenBackgroundTasksCompleted( CancellationToken cancellationToken ) => Task.CompletedTask;

    /// <summary>
    /// Event raised when a cache item is removed from the cache. Check the <see cref="CachingBackendFeatures.Events"/>
    /// property to see if the current <see cref="CachingBackend"/> supports events.
    /// </summary>
    public event EventHandler<CacheItemRemovedEventArgs> ItemRemoved
    {
        add
        {
            if ( !this.SupportedFeatures.Events )
            {
                throw new InvalidOperationException( string.Format( CultureInfo.InvariantCulture, "{0} does not support events.", this.GetType().Name ) );
            }

            this._itemRemoved += value;
        }
        remove => this._itemRemoved -= value;
    }

    /// <summary>
    /// Events raised when a dependency is invalidated. Check the <see cref="CachingBackendFeatures.Events"/>
    /// property to see if the current <see cref="CachingBackend"/> supports events.
    /// </summary>
    public event EventHandler<CacheDependencyInvalidatedEventArgs> DependencyInvalidated
    {
        add
        {
            if ( !this.SupportedFeatures.Events )
            {
                throw new InvalidOperationException( string.Format( CultureInfo.InvariantCulture, "{0} does not support events.", this.GetType().Name ) );
            }

            if ( !this.SupportedFeatures.Dependencies )
            {
                throw new InvalidOperationException( string.Format( CultureInfo.InvariantCulture, "{0} does not support dependencies.", this.GetType().Name ) );
            }

            this._dependencyInvalidated += value;
        }
        remove => this._dependencyInvalidated -= value;
    }

    private void Validate( CacheItem cacheItem )
    {
        if ( cacheItem.Configuration != null )
        {
            if ( cacheItem.Configuration.AbsoluteExpiration.HasValue && cacheItem.Configuration.SlidingExpiration.HasValue )
            {
                throw new ArgumentException( "The absolute and sliding expiration values are mutually exclusive. They cannot be set both at the same time." );
            }

            if ( (cacheItem.Configuration.AbsoluteExpiration.HasValue || cacheItem.Configuration.SlidingExpiration.HasValue)
                 && cacheItem.Configuration.Priority == CacheItemPriority.NotRemovable )
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The priority cannot be set to '{0}' when either the absolute expiration or sliding expiration is set as well.",
                        CacheItemPriority.NotRemovable ) );
            }
        }

        if ( !this.SupportedFeatures.Dependencies && !cacheItem.Dependencies.IsDefaultOrEmpty )
        {
            this.RequireFeature( this.SupportedFeatures.Dependencies, nameof(this.SupportedFeatures.Dependencies) );
        }
    }

    private static void RaiseEvent( WaitCallback action ) => ThreadPool.QueueUserWorkItem( action );

    /// <summary>
    /// Raises the <see cref="ItemRemoved"/> event given a <see cref="CacheItemRemovedEventArgs"/>.
    /// </summary>
    /// <param name="args">Event arguments.</param>
    protected void OnItemRemoved( CacheItemRemovedEventArgs args )
    {
        this.LogSource.Debug.Write( Formatted( "OnItemRemoved( backend=\"{Backend}\", Reason={Reason}, Key=\"{Key}\"", this, args.RemovedReason, args.Key ) );

        if ( this._itemRemoved != null )
        {
            RaiseEvent( _ => this._itemRemoved?.Invoke( this, args ) );
        }
    }

    /// <summary>
    /// Raises the <see cref="ItemRemoved"/> event, but lazily instantiates the <see cref="CacheItemRemovedEventArgs"/>
    /// if there is an event client.
    /// </summary>
    /// <param name="key">The key of the removed cache item.</param>
    /// <param name="reason">The reason of the removal.</param>
    /// <param name="sourceId">The <see cref="Guid"/> of the <see cref="CachingBackend"/> instance that removed the item,.
    /// or <see cref="Guid.Empty"/> if the source <see cref="CachingBackend"/> cannot be determined.</param>
    protected void OnItemRemoved( string key, CacheItemRemovedReason reason, Guid sourceId )
    {
        this.LogSource.Debug.Write( Formatted( "OnItemRemoved( Backend=\"{Backend}\", Reason={Reason}, Key=\"{Key}\"", this, reason, key ) );

        if ( this._itemRemoved != null )
        {
            RaiseEvent( _ => this._itemRemoved?.Invoke( this, new CacheItemRemovedEventArgs( key, reason, sourceId ) ) );
        }
    }

    /// <summary>
    /// Raises the <see cref="DependencyInvalidated"/> event , but lazily instantiates the <see cref="CacheDependencyInvalidatedEventArgs"/>
    /// if there is an event client.
    /// </summary>
    /// <param name="key">The key of the removed cache item.</param>
    /// <param name="sourceId">The <see cref="Guid"/> of the <see cref="CachingBackend"/> instance that removed the item,.
    /// or <see cref="Guid.Empty"/> if the source <see cref="CachingBackend"/> cannot be determined.</param>
    protected void OnDependencyInvalidated( string key, Guid sourceId )
    {
        this.LogSource.Debug.Write( Formatted( "OnDependencyInvalidated( Backend=\"{Backend}\", source={SourceId}, Key=\"{Key}\"", this, sourceId, key ) );

        if ( this._dependencyInvalidated != null )
        {
            RaiseEvent( _ => this._dependencyInvalidated?.Invoke( this, new CacheDependencyInvalidatedEventArgs( key, sourceId ) ) );
        }
    }

    /// <summary>
    /// Raises the <see cref="DependencyInvalidated"/> event given a <see cref="CacheDependencyInvalidatedEventArgs"/>.
    /// </summary>
    /// <param name="args">A <see cref="CacheDependencyInvalidatedEventArgs"/>.</param>
    protected void OnDependencyInvalidated( CacheDependencyInvalidatedEventArgs args )
    {
        this.LogSource.Debug.Write(
            Formatted( "OnDependencyInvalidated( Backend=\"{Backend}\", source={SourceId}, Key=\"{Key}\"", this, args.SourceId, args.Key ) );

        if ( this._dependencyInvalidated != null )
        {
            RaiseEvent( _ => this._dependencyInvalidated?.Invoke( this, args ) );
        }
    }

    /// <summary>
    /// Synchronously disposes the current <see cref="CachingBackend"/>.
    /// In case the <see cref="CachingBackend"/> has pending background tasks (typically cache non-blocking cache update tasks for distributed backends),
    /// it will wait until all tasks are processed.
    /// </summary>
    public void Dispose() => this.Dispose( true, default );

    /// <summary>
    /// Synchronously disposes the current <see cref="CachingBackend"/>. This overloads accepts a <see cref="CancellationToken"/>.
    /// In case the <see cref="CachingBackend"/> has pending background tasks (typically cache non-blocking cache update tasks for distributed backends),
    /// it will wait until all tasks are processed.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>. Cancelling this operation may cause cache inconsistencies, in case of distributed cache,
    /// or failure to properly dispose of other resources owned by this object.</param>
    public void Dispose( CancellationToken cancellationToken ) => this.Dispose( true, cancellationToken );

    /// <summary>
    /// Synchronously disposes the current <see cref="CachingBackend"/>, with a parameter instructing whether this method is called because
    /// of a call to the <see cref="Dispose()"/> method or because of object finalizing.
    /// </summary>
    /// <param name="disposing"><c>true</c> if this method is called because the <see cref="Dispose()"/> method has been called, or <c>false</c> if the object is being finalized.</param>
    protected void Dispose( bool disposing, CancellationToken cancellationToken )
    {
        using ( cancellationToken.Register( this._disposeCancellationTokenSource.Cancel ) )
        {
            if ( this.Status == CachingBackendStatus.Initializing )
            {
                this._initializeSemaphore.Wait( cancellationToken );
            }

            if ( this.TryChangeStatus( CachingBackendStatus.Initialized, CachingBackendStatus.Disposing ) ||
                 this.TryChangeStatus( CachingBackendStatus.Default, CachingBackendStatus.Disposing ) ||
                 this.TryChangeStatus( CachingBackendStatus.Failed, CachingBackendStatus.Disposing ) )
            {
                using ( var activity = this.LogSource.Default.OpenActivity( Formatted( "Disposing( backend = {Backend}", this ) ) )
                {
                    try
                    {
                        // Reset events to make sure that handlers don't make problems.
                        this._itemRemoved = null;
                        this._dependencyInvalidated = null;

                        this.DisposeCore( disposing, cancellationToken );

                        this._initializeSemaphore.Dispose();

                        if ( !this.TryChangeStatus( CachingBackendStatus.Disposing, CachingBackendStatus.Disposed ) )
                        {
#if DEBUG
                            throw new CachingAssertionFailedException();
#else
                        this.LogSource.Error.Write( Formatted( "Cannot dispose back-end: cannot change the status to Disposed." ) );

                        return;
#endif
                        }

                        this._disposeTask.SetResult( true );
                        activity.SetSuccess();
                    }
                    catch ( Exception e )
                    {
                        if ( !this.TryChangeStatus( CachingBackendStatus.Disposing, CachingBackendStatus.DisposeFailed ) )
                        {
#if DEBUG
                            throw new CachingAssertionFailedException();
#else
                        this.LogSource.Error.Write( Formatted( "Cannot dispose back-end: cannot change the status to DisposeFailed." ) );

                        return;
#endif
                        }

                        this._disposeTask.SetException( e );
                        activity.SetException( e );

                        throw;
                    }
                }
            }
            else
            {
                this._disposeTask.Task.Wait( cancellationToken );
            }
        }
    }

    /// <summary>
    /// Synchronously disposes the current <see cref="CachingBackend"/>. This protected method is part of the implementation API and is meant to be overridden in user code, not invoked.
    /// </summary>
    protected virtual void DisposeCore( bool disposing, CancellationToken cancellationToken )
        => this.WhenBackgroundTasksCompleted( this.DisposeCancellationToken ).Wait( this.DisposeCancellationToken );

    /// <summary> 
    /// Asynchronously dispose the current <see cref="CachingBackend"/>. This overload accepts a <see cref="CancellationToken"/>.
    /// In case the <see cref="CachingBackend"/> has pending background tasks (typically cache non-blocking cache update tasks for distributed backends),
    /// it will wait until all tasks are processed.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>. Cancelling this operation may cause cache inconsistencies, in case of distributed cache,
    /// or failure to properly dispose of other resources owned by this object.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public async ValueTask DisposeAsync( CancellationToken cancellationToken )
    {
#if NET6_0_OR_GREATER
        await using ( cancellationToken.Register( this._disposeCancellationTokenSource.Cancel ) )
#else
        using ( cancellationToken.Register( this._disposeCancellationTokenSource.Cancel ) )
#endif
        {
            if ( this.Status == CachingBackendStatus.Initializing )
            {
                await this._initializeSemaphore.WaitAsync( cancellationToken );
            }

            if ( this.TryChangeStatus( CachingBackendStatus.Initialized, CachingBackendStatus.Disposing ) ||
                 this.TryChangeStatus( CachingBackendStatus.Default, CachingBackendStatus.Disposing ) ||
                 this.TryChangeStatus( CachingBackendStatus.Failed, CachingBackendStatus.Disposing ) )
            {
                using ( var activity = this.LogSource.Default.OpenAsyncActivity( Formatted( "Disposing" ) ) )
                {
                    try
                    {
                        // Reset events to make sure that handlers don't make problems.
                        this._itemRemoved = null;
                        this._dependencyInvalidated = null;

                        await this.DisposeAsyncCore( cancellationToken );

                        if ( !this.TryChangeStatus( CachingBackendStatus.Disposing, CachingBackendStatus.Disposed ) )
                        {
                            throw new CachingAssertionFailedException();
                        }

                        this._initializeSemaphore.Dispose();

                        this._disposeTask.SetResult( true );
                        activity.SetSuccess();
                    }
                    catch ( Exception e )
                    {
                        if ( !this.TryChangeStatus( CachingBackendStatus.Disposing, CachingBackendStatus.DisposeFailed ) )
                        {
                            throw new CachingAssertionFailedException();
                        }

                        this._disposeTask.SetException( e );
                        activity.SetException( e );

                        throw;
                    }
                }
            }
            else
            {
                await this._disposeTask.Task;
            }
        }
    }

    /// <summary>
    /// Asynchronously disposes the current <see cref="CachingBackend"/>. This protected method is part of the implementation API and is meant to be overridden in user code, not invoked.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    protected virtual async ValueTask DisposeAsyncCore( CancellationToken cancellationToken )
    {
#if NET6_0_OR_GREATER
        await using ( cancellationToken.Register( this._disposeCancellationTokenSource.Cancel ) )
#else
        using ( cancellationToken.Register( this._disposeCancellationTokenSource.Cancel ) )
#endif
        {
            await this.WhenBackgroundTasksCompleted( this.DisposeCancellationToken );
        }
    }

    // Was [ExplicitCrossPackageInternal]. Used by Redis backend. Making protected for now.
    public virtual int BackgroundTaskExceptions => 0;

    public static CachingBackend Create( Func<CachingBackendBuilder, ConcreteCachingBackendBuilder> build, IServiceProvider? serviceProvider = null )
    {
        var builder = build( new CachingBackendBuilder( serviceProvider ) );

        var backend = builder.CreateBackend( new CreateBackendArgs { Layer = 1 } );

        return backend;
    }
}