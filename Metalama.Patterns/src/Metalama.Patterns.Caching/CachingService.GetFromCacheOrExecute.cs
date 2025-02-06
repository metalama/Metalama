// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace;
using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;
using System.ComponentModel;
using static Flashtrace.Messages.FormattedMessageBuilder;

namespace Metalama.Patterns.Caching;

[PublicAPI]
public partial class CachingService
{
    private ICacheItemConfiguration GetMergedMethodConfiguration( CachedMethodMetadata methodMetadata )
        => this.Profiles[methodMetadata.Configuration.ProfileName].GetMergedConfiguration( methodMetadata );

    [EditorBrowsable( EditorBrowsableState.Never )]
    public TResult? GetFromCacheOrExecute<TResult>(
        CachedMethodMetadata metadata,
        object? instance,
        object?[] args,
        Func<object?, object?[], object?> func,
        CacheItemConfiguration? configuration = null,
        CancellationToken cancellationToken = default )
    {
        var logSource = this.ServiceProvider.GetFlashtraceSource( metadata.Method.DeclaringType!, FlashtraceRole.Caching );

        object? result;

        using ( var activity = logSource.Default.OpenActivity( Formatted( "Processing invocation of method {Method}", metadata.Method ) ) )
        {
            try
            {
                var mergedConfiguration = configuration ?? this.GetMergedMethodConfiguration( metadata );

                if ( !mergedConfiguration.IsEnabled.GetValueOrDefault() )
                {
                    logSource.Debug.IfEnabled?.Write( Formatted( "Ignoring the caching aspect because caching is disabled for this profile." ) );

                    result = func( instance, args );
                }
                else
                {
                    var methodKey = this.KeyBuilder.BuildMethodKey(
                        metadata,
                        instance,
                        args );

                    logSource.Debug.IfEnabled?.Write( Formatted( "Key=\"{Key}\".", methodKey ) );

                    result = this.Frontend.GetOrAdd(
                        methodKey,
                        metadata.Method.ReturnType,
                        mergedConfiguration,
                        func,
                        instance,
                        args,
                        logSource );
                }

                activity.SetSuccess();
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }

        if ( metadata.ReturnValueCanBeNull )
        {
            return (TResult?) result;
        }
        else
        {
            return result == null ? default : (TResult) result;
        }
    }

    [EditorBrowsable( EditorBrowsableState.Never )]
    public async Task<TTaskResultType?> GetFromCacheOrExecuteTaskAsync<TTaskResultType>(
        CachedMethodMetadata metadata,
        object? instance,
        object?[] args,
        Func<object?, object?[], CancellationToken, Task<object?>> func,
        CacheItemConfiguration? configuration = null,
        CancellationToken cancellationToken = default )
    {
        // TODO: What about ConfigureAwait( false )?

        var logSource = this.ServiceProvider.GetFlashtraceSource( metadata.Method.DeclaringType!, FlashtraceRole.Caching );

        object? result;

        using ( var activity = logSource.Default.OpenAsyncActivity( Formatted( "Processing invocation of async method {Method}", metadata.Method ) ) )
        {
            try
            {
                var mergedConfiguration = configuration ?? this.GetMergedMethodConfiguration( metadata );

                if ( !mergedConfiguration.IsEnabled.GetValueOrDefault() )
                {
                    logSource.Debug.IfEnabled?.Write( Formatted( "Ignoring the caching aspect because caching is disabled for this profile." ) );

                    var task = func( instance, args, cancellationToken );

                    if ( !task.IsCompleted )
                    {
                        // We need to call LogActivity.Suspend and LogActivity.Resume manually because we're in an aspect,
                        // and the await instrumentation policy is not applied.
                        activity.Suspend();

                        try
                        {
                            result = await task;
                        }
                        finally
                        {
                            activity.Resume();
                        }
                    }
                    else
                    {
                        // Don't wrap any exception.
                        result = task.GetAwaiter().GetResult();
                    }
                }
                else
                {
                    var methodKey = Default.KeyBuilder.BuildMethodKey(
                        metadata,
                        instance,
                        args );

                    logSource.Debug.IfEnabled?.Write( Formatted( "Key=\"{Key}\".", methodKey ) );

                    var task = this.Frontend.GetOrAddAsync(
                        methodKey,
                        metadata.AwaitableResultType!,
                        mergedConfiguration,
                        func,
                        instance,
                        args,
                        logSource,
                        cancellationToken );

                    if ( !task.IsCompleted )
                    {
                        // We need to call LogActivity.Suspend and LogActivity.Resume manually because we're in an aspect,
                        // and the await instrumentation policy is not applied.
                        activity.Suspend();

                        try
                        {
                            result = await task;
                        }
                        finally
                        {
                            activity.Resume();
                        }
                    }
                    else
                    {
                        // Don't wrap any exception.
                        result = task.GetAwaiter().GetResult();
                    }
                }

                activity.SetSuccess();
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }

        if ( metadata.ReturnValueCanBeNull )
        {
            return (TTaskResultType?) result;
        }
        else
        {
            return result == null ? default : (TTaskResultType) result;
        }
    }

    [EditorBrowsable( EditorBrowsableState.Never )]
    public async ValueTask<TTaskResultType?> GetFromCacheOrExecuteValueTaskAsync<TTaskResultType>(
        CachedMethodMetadata metadata,
        object? instance,
        object?[] args,
        Func<object?, object?[], CancellationToken, ValueTask<object?>> func,
        CacheItemConfiguration? configuration = null,
        CancellationToken cancellationToken = default )
    {
        // TODO: What about ConfigureAwait( false )?

        var logSource = this.ServiceProvider.GetFlashtraceSource( metadata.Method.DeclaringType!, FlashtraceRole.Caching );

        object? result;

        using ( var activity = logSource.Default.OpenAsyncActivity( Formatted( "Processing invocation of async method {Method}", metadata.Method ) ) )
        {
            try
            {
                var mergedConfiguration = configuration ?? this.GetMergedMethodConfiguration( metadata );

                if ( !mergedConfiguration.IsEnabled.GetValueOrDefault() )
                {
                    logSource.Debug.IfEnabled?.Write( Formatted( "Ignoring the caching aspect because caching is disabled for this profile." ) );

                    var task = func( instance, args, cancellationToken );

                    if ( !task.IsCompleted )
                    {
                        // We need to call LogActivity.Suspend and LogActivity.Resume manually because we're in an aspect,
                        // and the await instrumentation policy is not applied.
                        activity.Suspend();

                        try
                        {
                            result = await task;
                        }
                        finally
                        {
                            activity.Resume();
                        }
                    }
                    else
                    {
                        // Don't wrap any exception.
                        result = task.GetAwaiter().GetResult();
                    }
                }
                else
                {
                    var methodKey = Default.KeyBuilder.BuildMethodKey(
                        metadata,
                        instance,
                        args );

                    logSource.Debug.IfEnabled?.Write( Formatted( "Key=\"{Key}\".", methodKey ) );

                    var task = this.Frontend.GetOrAddAsync(
                        methodKey,
                        metadata.AwaitableResultType!,
                        mergedConfiguration,
                        func,
                        instance,
                        args,
                        logSource,
                        cancellationToken );

                    if ( !task.IsCompleted )
                    {
                        // We need to call LogActivity.Suspend and LogActivity.Resume manually because we're in an aspect,
                        // and the await instrumentation policy is not applied.
                        activity.Suspend();

                        try
                        {
                            result = await task;
                        }
                        finally
                        {
                            activity.Resume();
                        }
                    }
                    else
                    {
                        // Don't wrap any exception.
                        result = task.GetAwaiter().GetResult();
                    }
                }

                activity.SetSuccess();
            }
            catch ( Exception e )
            {
                activity.SetException( e );

                throw;
            }
        }

        if ( metadata.ReturnValueCanBeNull )
        {
            return (TTaskResultType?) result;
        }
        else
        {
            return result == null ? default : (TTaskResultType) result;
        }
    }
}