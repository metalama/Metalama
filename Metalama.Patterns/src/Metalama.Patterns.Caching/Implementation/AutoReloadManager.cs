// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace;
using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;
using System.Collections.Concurrent;
using static Flashtrace.Messages.FormattedMessageBuilder;

namespace Metalama.Patterns.Caching.Implementation;

internal sealed class AutoReloadManager : IDisposable, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, AutoRefreshSubscription> _subscriptions = new();
    private readonly BackgroundTaskScheduler _backgroundTaskScheduler;
    private readonly CachingService _cachingService;
    private readonly ConcurrentDictionary<CachingBackend, int> _monitoredBackends = new();

    public AutoReloadManager( CachingService cachingService )
    {
        this._cachingService = cachingService;
        this._backgroundTaskScheduler = new BackgroundTaskScheduler( cachingService.ServiceProvider );
    }

    private void OnItemRemoved( object? sender, CacheItemRemovedEventArgs args )
    {
        var backend = (CachingBackend) sender!;
        var key = args.Key;

        if ( this._subscriptions.TryGetValue( key, out var autoRefreshInfo ) )
        {
            if ( autoRefreshInfo.IsAsync )
            {
                this._backgroundTaskScheduler.EnqueueBackgroundTask( ct => this.AutoRefreshCoreAsync( backend, key, autoRefreshInfo, ct ) );
            }
            else
            {
                this._backgroundTaskScheduler.EnqueueBackgroundTask( ct => Task.Run( () => this.AutoRefreshCore( backend, key, autoRefreshInfo ), ct ) );
            }
        }
    }

    internal void SubscribeAutoRefresh(
        CachingBackend backend,
        string key,
        Type valueType,
        ICacheItemConfiguration configuration,
        Func<object?> valueProvider,
        FlashtraceSource logger,
        bool isAsync )
    {
        if ( !backend.SupportedFeatures.Events )
        {
            logger.Warning.Write( Formatted( "The backend {Backend} does not support auto-refresh.", backend ) );

            return;
        }

        // TODO: We may want to preemptively renew the cache item before it gets removed, otherwise there could be latency.

        this._subscriptions.GetOrAdd(
            key,
            _ =>
            {
                if ( this._monitoredBackends.AddOrUpdate( backend, _ => 1, ( _, count ) => count + 1 ) == 1 )
                {
                    backend.ItemRemoved += this.OnItemRemoved;
                }

                return new AutoRefreshSubscription( configuration, valueType, valueProvider, logger, isAsync );
            } );

        // NOTE: We never remove things from autoRefreshInfos. AutoRefresh keys are there forever, they are never evicted.
    }

    private void AutoRefreshCore( CachingBackend backend, string key, AutoRefreshSubscription subscription )
    {
        using ( var activity = subscription.Logger.Default.IfEnabled?.OpenActivity( Formatted( "Auto-refreshing: {Key}", key ) ) )
        {
            try
            {
                using ( var context = CachingContext.OpenCacheContext( key ) )
                {
                    var value = subscription.ValueProvider.Invoke();

                    this._cachingService.Frontend.SetItem( backend, key, value, subscription.ReturnType, subscription.Configuration, context );
                }

                activity?.SetSuccess();
            }
            catch ( Exception e ) when ( activity != null )
            {
                activity.Value.SetException( e );
            }
        }
    }

    private async Task AutoRefreshCoreAsync( CachingBackend backend, string key, AutoRefreshSubscription subscription, CancellationToken cancellationToken )
    {
        using ( var activity = subscription.Logger.Default.OpenAsyncActivity( Formatted( "Auto-refreshing: {Key}", key ) ) )
        {
            try
            {
                using ( var context = CachingContext.OpenCacheContext( key ) )
                {
                    var invokeValueProviderTask = (Task<object?>?) subscription.ValueProvider.Invoke();
                    var value = invokeValueProviderTask == null ? null : await invokeValueProviderTask;

                    await this._cachingService.Frontend.SetItemAsync(
                        backend,
                        key,
                        value,
                        subscription.ReturnType,
                        subscription.Configuration,
                        context,
                        cancellationToken );
                }

                activity.SetSuccess();
            }
            catch ( Exception e )
            {
                activity.SetException( e );
            }
        }
    }

    private sealed record AutoRefreshSubscription(
        ICacheItemConfiguration Configuration,
        Type ReturnType,
        Func<object?> ValueProvider,
        FlashtraceSource Logger,
        bool IsAsync );

    private void Unsubscribe()
    {
        foreach ( var backend in this._monitoredBackends.Keys )
        {
            backend.ItemRemoved -= this.OnItemRemoved;
        }
    }

    [PublicAPI]
    public void Dispose( CancellationToken cancellationToken )
    {
        this.Unsubscribe();
        this._backgroundTaskScheduler.Dispose( cancellationToken );
    }

    [PublicAPI]
    public async ValueTask DisposeAsync( CancellationToken cancellationToken )
    {
        this.Unsubscribe();
        await this._backgroundTaskScheduler.DisposeAsync( cancellationToken );
    }

    public void Dispose() => this.Dispose( default );

    public ValueTask DisposeAsync() => this.DisposeAsync( default );
}