// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Metalama.Patterns.Caching.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
// ReSharper disable MethodHasAsyncOverload

namespace Metalama.Patterns.Caching.Tests.Backends;

public sealed class NonBlockingCachingBackendEnhancerTests : IDisposable
{
    private const string _testKey = "testKey";
    private const string _testValue = "testValue";
    private const string _testDependency = "testDependency";

    private readonly ServiceProvider _serviceProvider;

    public NonBlockingCachingBackendEnhancerTests()
    {
        var services = new ServiceCollection();
        this._serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        this._serviceProvider.Dispose();
    }

    private (NonBlockingCachingBackendEnhancer enhancer, TrackingCachingBackend underlying) CreateEnhancer()
    {
        var underlying = new TrackingCachingBackend( "tracking", this._serviceProvider );
        var enhancer = new NonBlockingCachingBackendEnhancer( underlying );

        return (enhancer, underlying);
    }

    #region Non-Blocking Write Operations Tests

    [Fact]
    public async Task SetItem_Sync_EnqueuesBackgroundTask()
    {
        var (enhancer, underlying) = this.CreateEnhancer();
        await using var _ = enhancer;

        var item = new CacheItem( _testValue );

        // This should return immediately (non-blocking)
        enhancer.SetItem( _testKey, item );

        // Wait for background task to complete
        await enhancer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        // Underlying backend should have received the item
        Assert.True( underlying.SetItemCalled );
        Assert.Equal( _testKey, underlying.LastSetKey );
    }

    [Fact]
    public async Task SetItemAsync_EnqueuesBackgroundTask()
    {
        var (enhancer, underlying) = this.CreateEnhancer();
        await using var _ = enhancer;

        var item = new CacheItem( _testValue );

        // This should return immediately (non-blocking)
        await enhancer.SetItemAsync( _testKey, item, CancellationToken.None );

        // Wait for background task to complete
        await enhancer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        // Underlying backend should have received the item
        Assert.True( underlying.SetItemCalled );
    }

    [Fact]
    public async Task RemoveItem_Sync_EnqueuesBackgroundTask()
    {
        var (enhancer, underlying) = this.CreateEnhancer();
        await using var _ = enhancer;

        enhancer.RemoveItem( _testKey );

        await enhancer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        Assert.True( underlying.RemoveItemCalled );
        Assert.Equal( _testKey, underlying.LastRemovedKey );
    }

    [Fact]
    public async Task RemoveItemAsync_EnqueuesBackgroundTask()
    {
        var (enhancer, underlying) = this.CreateEnhancer();
        await using var _ = enhancer;

        await enhancer.RemoveItemAsync( _testKey, CancellationToken.None );

        await enhancer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        Assert.True( underlying.RemoveItemCalled );
    }

    [Fact]
    public async Task InvalidateDependency_Sync_EnqueuesBackgroundTask()
    {
        var (enhancer, underlying) = this.CreateEnhancer();
        await using var _ = enhancer;

        enhancer.InvalidateDependency( _testDependency );

        await enhancer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        Assert.True( underlying.InvalidateDependencyCalled );
        Assert.Equal( _testDependency, underlying.LastInvalidatedDependency );
    }

    [Fact]
    public async Task InvalidateDependencyAsync_EnqueuesBackgroundTask()
    {
        var (enhancer, underlying) = this.CreateEnhancer();
        await using var _ = enhancer;

        await enhancer.InvalidateDependencyAsync( _testDependency, CancellationToken.None );

        await enhancer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        Assert.True( underlying.InvalidateDependencyCalled );
    }

    [Fact]
    public async Task Clear_Sync_EnqueuesBackgroundTask()
    {
        var (enhancer, underlying) = this.CreateEnhancer();
        await using var _ = enhancer;

        enhancer.Clear();

        await enhancer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        Assert.True( underlying.ClearCalled );
    }

    [Fact]
    public async Task ClearAsync_EnqueuesBackgroundTask()
    {
        var (enhancer, underlying) = this.CreateEnhancer();
        await using var _ = enhancer;

        await enhancer.ClearAsync( cancellationToken: CancellationToken.None );

        await enhancer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        Assert.True( underlying.ClearCalled );
    }

    #endregion

    #region Read Operations (Should Remain Blocking) Tests

    [Fact]
    public async Task GetItem_Sync_RemainsBlocking()
    {
        var (enhancer, underlying) = this.CreateEnhancer();
        await using var _ = enhancer;

        // First set the item through the underlying backend directly
        await underlying.SetItemAsync( _testKey, new CacheItem( _testValue ), CancellationToken.None );
        underlying.ResetTracking();

        // GetItem should be blocking and return result immediately
        var result = enhancer.GetItem( _testKey );

        // Should have the result immediately, not async
        Assert.NotNull( result );
        Assert.Equal( _testValue, result.Value );
        Assert.True( underlying.GetItemCalled );
    }

    [Fact]
    public async Task GetItemAsync_RemainsBlocking()
    {
        var (enhancer, underlying) = this.CreateEnhancer();
        await using var _ = enhancer;

        // First set the item through the underlying backend directly
        await underlying.SetItemAsync( _testKey, new CacheItem( _testValue ), CancellationToken.None );
        underlying.ResetTracking();

        // GetItemAsync should return result directly (not enqueued)
        var result = await enhancer.GetItemAsync( _testKey, cancellationToken: CancellationToken.None );

        Assert.NotNull( result );
        Assert.Equal( _testValue, result.Value );
        Assert.True( underlying.GetItemCalled );
    }

    #endregion

    #region Feature Tests

    [Fact]
    public async Task SupportedFeatures_Blocking_ReturnsFalse()
    {
        var (enhancer, _) = this.CreateEnhancer();
        await using var _ = enhancer;

        Assert.False( enhancer.SupportedFeatures.Blocking );
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public async Task DisposeAsync_CompletesEnqueuedTasks()
    {
        var (enhancer, underlying) = this.CreateEnhancer();

        // Enqueue several tasks
        for ( var i = 0; i < 5; i++ )
        {
            enhancer.SetItem( $"key{i}", new CacheItem( $"value{i}" ) );
        }

        // Dispose should wait for all tasks to complete
        await enhancer.DisposeAsync();

        // All 5 items should have been set
        Assert.Equal( 5, underlying.SetItemCount );
    }

    [Fact]
    public void Dispose_CompletesEnqueuedTasks()
    {
        var (enhancer, underlying) = this.CreateEnhancer();

        // Enqueue several tasks
        for ( var i = 0; i < 5; i++ )
        {
            enhancer.SetItem( $"key{i}", new CacheItem( $"value{i}" ) );
        }

        // Dispose should wait for all tasks to complete
        enhancer.Dispose();

        // All 5 items should have been set
        Assert.Equal( 5, underlying.SetItemCount );
    }

    #endregion

    #region Multiple Operations Tests

    [Fact]
    public async Task MultipleOperations_ExecuteInBackground()
    {
        var (enhancer, underlying) = this.CreateEnhancer();
        await using var _ = enhancer;

        // Enqueue various operations
        enhancer.SetItem( "key1", new CacheItem( "value1" ) );
        enhancer.SetItem( "key2", new CacheItem( "value2" ) );
        enhancer.RemoveItem( "key1" );
        enhancer.InvalidateDependency( "dep1" );
        enhancer.Clear();

        // All operations should complete
        await enhancer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        Assert.Equal( 2, underlying.SetItemCount );
        Assert.True( underlying.RemoveItemCalled );
        Assert.True( underlying.InvalidateDependencyCalled );
        Assert.True( underlying.ClearCalled );
    }

    #endregion
}
