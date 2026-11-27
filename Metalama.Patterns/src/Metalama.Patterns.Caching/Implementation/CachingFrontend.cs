// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Locking;
using System.ComponentModel;
using static Flashtrace.Messages.FormattedMessageBuilder;

namespace Metalama.Patterns.Caching.Implementation;

[EditorBrowsable( EditorBrowsableState.Never )]
internal sealed class CachingFrontend
{
    private readonly CachingService _cachingService;

    public CachingFrontend( CachingService cachingService )
    {
        this._cachingService = cachingService;
    }

    public object? GetOrAdd(
        string key,
        Type valueType,
        ICacheItemConfiguration configuration,
        Func<object?, object?[], object?> invokeOriginalMethod,
        object? instance,
        object?[] args,
        FlashtraceSource logger )
    {
        ILockHandle? lockHandle = null;
        CacheItem? item = null;

        var profile = this._cachingService.Profiles[configuration.ProfileName ?? CachingProfile.DefaultName];
        var backend = profile.Backend;

        try
        {
            if ( (CachingContext.Current.Kind & CachingContextKind.Refresh) == 0 )
            {
                item = backend.GetItem( key );

                if ( item == null )
                {
                    // The item was not found in the cache, so we have to acquire a lock.
                    lockHandle = profile.LockingStrategy.GetLock( key );

                    if ( lockHandle.Acquire( TimeSpan.Zero, CancellationToken.None ) )
                    {
                        // The lock was acquired without waiting so we are sure to be the first reader and we don't need to
                        // do a second cache lookup.
                    }
                    else if ( lockHandle.Acquire( profile.AcquireLockTimeout, CancellationToken.None ) )
                    {
                        // We had to wait to acquire the lock, so another reader may have put the item in the cache.
                        // Do a second cache lookup.

                        item = backend.GetItem( key );
                    }
                    else
                    {
                        // Time out condition.
                        profile.OnLockTimeout( new LockTimeoutContext( key, lockHandle, backend, this._cachingService ) );
                    }
                }

                if ( item != null )
                {
                    var valueAdapter = this._cachingService.ValueAdapters.Get( valueType );

                    if ( valueAdapter != null )
                    {
                        var unwrappedValue = valueAdapter.GetExposedValue( item.Value );
                        item = item with { Value = unwrappedValue };
                    }
                }
            }
            else
            {
                // When we recache, we have to acquire the lock without doing a cache lookup.
                lockHandle = profile.LockingStrategy.GetLock( key );

                if ( !lockHandle.Acquire( profile.AcquireLockTimeout, CancellationToken.None ) )
                {
                    // Time out condition.
                    profile.OnLockTimeout( new LockTimeoutContext( key, lockHandle, backend, this._cachingService ) );
                }
            }

            object? value;

            if ( item == null )
            {
#if DEBUG

                // At this point, we assume we own the lock.
                if ( lockHandle == null )
                {
                    throw new CachingAssertionFailedException();
                }
#endif

                // Cache miss.
                if ( configuration.AutoReload.GetValueOrDefault() )
                {
                    object? GetValue() => invokeOriginalMethod( instance, args );

                    this._cachingService.AutoReloadManager.SubscribeAutoRefresh( backend, key, valueType, configuration, GetValue, logger, false );
                }

                logger.Debug.IfEnabled?.Write( Formatted( "Cache miss: Key=\"{Key}\".", key ) );

                using ( var context = CachingContext.OpenCacheContext( key, GetComputationStartTime( backend ) ) )
                {
                    value = invokeOriginalMethod( instance, args );

                    value = this.SetItem( profile.Backend, key, value, valueType, configuration, context );

                    context.AddDependenciesToParent( profile.Backend );
                }
            }
            else
            {
                // Cache hit.

                logger.Debug.IfEnabled?.Write( Formatted( "Cache hit: Key=\"{Key}\".", key ) );

                AddChildDependency( backend, key );

                value = item.Value;
            }

            return value;
        }
        finally
        {
            if ( lockHandle != null )
            {
                lockHandle.Release();
                lockHandle.Dispose();
            }
        }
    }

    public async Task<object?> GetOrAddAsync(
        string key,
        Type valueType,
        ICacheItemConfiguration configuration,
        Func<object?, object?[], CancellationToken, Task<object?>> invokeOriginalMethod,
        object? instance,
        object?[] args,
        FlashtraceSource logger,
        CancellationToken cancellationToken )
    {
        // Keep any changes in logic in sync with other overloads of GetOrAddAsync.

        CacheItem? item = null;
        ILockHandle? lockHandle = null;

        var profile = this._cachingService.Profiles[configuration.ProfileName ?? CachingProfile.DefaultName];
        var backend = profile.Backend;

        try
        {
            if ( (CachingContext.Current.Kind & CachingContextKind.Refresh) == 0 )
            {
                item = await backend.GetItemAsync( key, cancellationToken: cancellationToken );

                if ( item == null )
                {
                    // The item was not found in the cache, so we have to acquire a lock.
                    lockHandle = profile.LockingStrategy.GetLock( key );

                    if ( await lockHandle.AcquireAsync( TimeSpan.Zero, CancellationToken.None ) )
                    {
                        // The lock was acquired without waiting so we are sure to be the first reader and we don't need to
                        // do a second cache lookup.
                    }
                    else if ( await lockHandle.AcquireAsync( profile.AcquireLockTimeout, CancellationToken.None ) )
                    {
                        // We had to wait to acquire the lock, so another reader may have put the item in the cache.
                        // Do a second cache lookup.

                        item = await backend.GetItemAsync( key, cancellationToken: cancellationToken );
                    }
                    else
                    {
                        // Time out condition.
                        profile.OnLockTimeout( new LockTimeoutContext( key, lockHandle, backend, this._cachingService ) );
                    }
                }

                if ( item != null )
                {
                    var valueAdapter = this._cachingService.ValueAdapters.Get( valueType );

                    if ( valueAdapter != null )
                    {
                        var unwrappedValue = valueAdapter.GetExposedValue( item.Value );
                        item = item with { Value = unwrappedValue };
                    }
                }
            }
            else
            {
                // When we recache, we have to acquire the lock without doing a cache lookup.
                lockHandle = profile.LockingStrategy.GetLock( key );

                if ( !await lockHandle.AcquireAsync( profile.AcquireLockTimeout, CancellationToken.None ) )
                {
                    // Time out condition.
                    profile.OnLockTimeout( new LockTimeoutContext( key, lockHandle, backend, this._cachingService ) );
                }
            }

            object? value;

            if ( item == null )
            {
                // Cache miss.

#if DEBUG

                // At this point, we assume we own the lock.
                if ( lockHandle == null )
                {
                    throw new CachingAssertionFailedException();
                }
#endif

                if ( configuration.AutoReload.GetValueOrDefault() )
                {
                    Task<object?> GetValue() => invokeOriginalMethod( instance, args, cancellationToken );

                    this._cachingService.AutoReloadManager.SubscribeAutoRefresh( backend, key, valueType, configuration, GetValue, logger, true );
                }

                logger.Debug.IfEnabled?.Write( Formatted( "Cache miss: Key=\"{Key}\".", key ) );

                using ( var context = CachingContext.OpenCacheContext( key, await GetComputationStartTimeAsync( backend, cancellationToken ) ) )
                {
                    var invokeValueProviderTask = invokeOriginalMethod( instance, args, cancellationToken );

                    value = await invokeValueProviderTask;

                    var invokeSetItemAsyncTask = this.SetItemAsync( profile.Backend, key, value, valueType, configuration, context, cancellationToken );

                    value = await invokeSetItemAsyncTask;

                    context.AddDependenciesToParent( profile.Backend );
                }
            }
            else
            {
                // Cache hit.

                logger.Debug.IfEnabled?.Write( Formatted( "Cache hit: Key=\"{Key}\".", key ) );

                AddChildDependency( backend, key );

                value = item.Value;
            }

            return value;
        }
        finally
        {
            if ( lockHandle != null )
            {
                await lockHandle.ReleaseAsync();
                lockHandle.Dispose();
            }
        }
    }

    public async ValueTask<object?> GetOrAddAsync(
        string key,
        Type valueType,
        ICacheItemConfiguration configuration,
        Func<object?, object?[], CancellationToken, ValueTask<object?>> invokeOriginalMethod,
        object? instance,
        object?[] args,
        FlashtraceSource logger,
        CancellationToken cancellationToken )
    {
        // Keep any changes in logic in sync with other overloads of GetOrAddAsync.

        CacheItem? item = null;
        ILockHandle? lockHandle = null;

        var profile = this._cachingService.Profiles[configuration.ProfileName ?? CachingProfile.DefaultName];
        var backend = profile.Backend;

        try
        {
            if ( (CachingContext.Current.Kind & CachingContextKind.Refresh) == 0 )
            {
                item = await backend.GetItemAsync( key, cancellationToken: cancellationToken );

                if ( item == null )
                {
                    // The item was not found in the cache, so we have to acquire a lock.
                    lockHandle = profile.LockingStrategy.GetLock( key );

                    if ( await lockHandle.AcquireAsync( TimeSpan.Zero, CancellationToken.None ) )
                    {
                        // The lock was acquired without waiting so we are sure to be the first reader and we don't need to
                        // do a second cache lookup.
                    }
                    else if ( await lockHandle.AcquireAsync( profile.AcquireLockTimeout, CancellationToken.None ) )
                    {
                        // We had to wait to acquire the lock, so another reader may have put the item in the cache.
                        // Do a second cache lookup.

                        item = await backend.GetItemAsync( key, cancellationToken: cancellationToken );
                    }
                    else
                    {
                        // Time out condition.
                        profile.OnLockTimeout( new LockTimeoutContext( key, lockHandle, backend, this._cachingService ) );
                    }
                }

                if ( item != null )
                {
                    var valueAdapter = this._cachingService.ValueAdapters.Get( valueType );

                    if ( valueAdapter != null )
                    {
                        var unwrappedValue = valueAdapter.GetExposedValue( item.Value );
                        item = item with { Value = unwrappedValue };
                    }
                }
            }
            else
            {
                // When we recache, we have to acquire the lock without doing a cache lookup.
                lockHandle = profile.LockingStrategy.GetLock( key );

                if ( !await lockHandle.AcquireAsync( profile.AcquireLockTimeout, CancellationToken.None ) )
                {
                    // Time out condition.
                    profile.OnLockTimeout( new LockTimeoutContext( key, lockHandle, backend, this._cachingService ) );
                }
            }

            object? value;

            if ( item == null )
            {
                // Cache miss.

#if DEBUG

                // At this point, we assume we own the lock.
                if ( lockHandle == null )
                {
                    throw new CachingAssertionFailedException();
                }
#endif

                if ( configuration.AutoReload.GetValueOrDefault() )
                {
                    Task<object?> GetValue() => invokeOriginalMethod( instance, args, cancellationToken ).AsTask();

                    this._cachingService.AutoReloadManager.SubscribeAutoRefresh( backend, key, valueType, configuration, GetValue, logger, true );
                }

                logger.Debug.IfEnabled?.Write( Formatted( "Cache miss: Key=\"{Key}\".", key ) );

                using ( var context = CachingContext.OpenCacheContext( key, await GetComputationStartTimeAsync( backend, cancellationToken ) ) )
                {
                    var invokeValueProviderTask = invokeOriginalMethod( instance, args, cancellationToken );

                    value = await invokeValueProviderTask;

                    var invokeSetItemAsyncTask = this.SetItemAsync( profile.Backend, key, value, valueType, configuration, context, cancellationToken );

                    value = await invokeSetItemAsyncTask;

                    context.AddDependenciesToParent( profile.Backend );
                }
            }
            else
            {
                // Cache hit.

                logger.Debug.IfEnabled?.Write( Formatted( "Cache hit: Key=\"{Key}\".", key ) );

                AddChildDependency( backend, key );

                value = item.Value;
            }

            return value;
        }
        finally
        {
            if ( lockHandle != null )
            {
                await lockHandle.ReleaseAsync();
                lockHandle.Dispose();
            }
        }
    }

    /// <summary>
    /// Reads the clock of the backend before the value of an item is computed, so that the item can be stored with the
    /// <c>ifUnmodifiedSince</c> precondition of <see cref="CachingBackend.SetItem"/>.
    /// </summary>
    /// <param name="backend">The backend.</param>
    /// <returns>The current time of the backend, or <see langword="null"/> when the backend does not support the feature.</returns>
    internal static DateTimeOffset? GetComputationStartTime( CachingBackend backend )
        => backend.SupportedFeatures.SetItemIfUnmodifiedSince ? backend.Clock.GetCurrentTime() : null;

    /// <summary>
    /// Asynchronously reads the clock of the backend before the value of an item is computed. See <see cref="GetComputationStartTime"/>.
    /// </summary>
    /// <param name="backend">The backend.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The current time of the backend, or <see langword="null"/> when the backend does not support the feature.</returns>
    internal static async ValueTask<DateTimeOffset?> GetComputationStartTimeAsync( CachingBackend backend, CancellationToken cancellationToken )
        => backend.SupportedFeatures.SetItemIfUnmodifiedSince ? await backend.Clock.GetCurrentTimeAsync( cancellationToken ) : null;

    private static void AddChildDependency( CachingBackend backend, string key )
    {
        if ( backend.SupportedFeatures.Dependencies )
        {
            CachingContext.Current.AddDependency( key );
        }
    }

    public object? SetItem(
        CachingBackend backend,
        string key,
        object? value,
        Type valueType,
        ICacheItemConfiguration configuration,
        CachingContext context )
    {
        var exposedValue = value;

        if ( value != null )
        {
            var valueAdapter = this._cachingService.ValueAdapters.Get( valueType );

            if ( valueAdapter != null )
            {
                value = valueAdapter.GetStoredValue( value );
                exposedValue = valueAdapter.GetExposedValue( value );
            }
        }

        var cacheItem = new CacheItem( value, [..context.Dependencies], configuration );

        // The item is not stored when it has been modified since its computation started.
        backend.SetItem( key, cacheItem, context.ComputationStartTime );

        return exposedValue;
    }

    public async Task<object?> SetItemAsync(
        CachingBackend backend,
        string key,
        object? value,
        Type valueType,
        ICacheItemConfiguration configuration,
        CachingContext context,
        CancellationToken cancellationToken )
    {
        var exposedValue = value;

        if ( value != null )
        {
            var valueAdapter = this._cachingService.ValueAdapters.Get( valueType );

            if ( valueAdapter != null )
            {
                if ( valueAdapter.IsAsyncSupported )
                {
                    var getStoredValueTask = valueAdapter.GetStoredValueAsync( value, cancellationToken );
                    value = await getStoredValueTask;
                }
                else
                {
                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                    value = valueAdapter.GetStoredValue( value );
                }

                exposedValue = valueAdapter.GetExposedValue( value );
            }
        }

        var cacheItem = new CacheItem( value, [..context.Dependencies], configuration );

        // The item is not stored when it has been modified since its computation started.
        await backend.SetItemAsync( key, cacheItem, context.ComputationStartTime, cancellationToken );

        return exposedValue;
    }
}