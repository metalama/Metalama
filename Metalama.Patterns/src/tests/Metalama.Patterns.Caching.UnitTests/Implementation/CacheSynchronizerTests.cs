// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Metalama.Patterns.Caching.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Implementation;

public sealed class CacheSynchronizerTests : IDisposable
{
    private const string _testKey = "testKey";
    private const string _testValue = "testValue";
    private const string _testDependency = "myDependency";
    private const string _testBackendName = "TestBackend";

    private readonly ServiceProvider _serviceProvider;

    public CacheSynchronizerTests()
    {
        var services = new ServiceCollection();
        this._serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        this._serviceProvider.Dispose();
    }

    #region Message Prefix Tests

    [Fact]
    public async Task OnMessageReceived_CorrectPrefix_ProcessesMessage()
    {
        await using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = _testBackendName } ),
            this._serviceProvider );

        var synchronizer = new TestCacheSynchronizer( backend, new CacheSynchronizerConfiguration { Prefix = "test" } );
        await using var _ = synchronizer;

        // Set an item that we'll try to remove
        await backend.SetItemAsync( _testKey, new CacheItem( _testValue ), CancellationToken.None );

        // Construct a valid message with the correct prefix
        var sourceId = Guid.NewGuid(); // Different source ID than the backend
        var message = $"test:item:{sourceId}:{_testKey}";

        synchronizer.ProcessMessage( message );

        // Wait for background tasks
        await synchronizer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        // The item should have been removed
        var item = await backend.GetItemAsync( _testKey, cancellationToken: CancellationToken.None );
        Assert.Null( item );
    }

    [Fact]
    public async Task OnMessageReceived_WrongPrefix_IgnoresMessage()
    {
        await using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = _testBackendName } ),
            this._serviceProvider );

        var synchronizer = new TestCacheSynchronizer( backend, new CacheSynchronizerConfiguration { Prefix = "correct" } );
        await using var _ = synchronizer;

        // Set an item
        await backend.SetItemAsync( _testKey, new CacheItem( _testValue ), CancellationToken.None );

        // Construct a message with wrong prefix
        var sourceId = Guid.NewGuid();
        var message = $"wrong:item:{sourceId}:{_testKey}";

        synchronizer.ProcessMessage( message );

        // Wait a bit
        await synchronizer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        // The item should still exist (message was ignored)
        var item = await backend.GetItemAsync( _testKey, cancellationToken: CancellationToken.None );
        Assert.NotNull( item );
    }

    #endregion

    #region Message Kind Tests

    [Fact]
    public async Task OnMessageReceived_ItemRemoved_RemovesItem()
    {
        await using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = _testBackendName } ),
            this._serviceProvider );

        var synchronizer = new TestCacheSynchronizer( backend, new CacheSynchronizerConfiguration() );
        await using var _ = synchronizer;

        const string itemKey = "itemKey";
        await backend.SetItemAsync( itemKey, new CacheItem( _testValue ), CancellationToken.None );

        var sourceId = Guid.NewGuid();
        var message = $"invalidate:item:{sourceId}:{itemKey}";

        synchronizer.ProcessMessage( message );
        await synchronizer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        var item = await backend.GetItemAsync( itemKey, cancellationToken: CancellationToken.None );
        Assert.Null( item );
    }

    [Fact]
    public async Task OnMessageReceived_DependencyInvalidation_InvalidatesDependency()
    {
        await using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = _testBackendName } ),
            this._serviceProvider );

        var synchronizer = new TestCacheSynchronizer( backend, new CacheSynchronizerConfiguration() );
        await using var _ = synchronizer;

        // Add an item with a dependency
        const string itemWithDep = "itemWithDep";
        await backend.SetItemAsync( itemWithDep, new CacheItem( _testValue, [_testDependency] ), CancellationToken.None );

        // Invalidate the dependency from a different source
        var sourceId = Guid.NewGuid();
        var message = $"invalidate:dependency:{sourceId}:{_testDependency}";

        synchronizer.ProcessMessage( message );
        await synchronizer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        // The item should be gone because its dependency was invalidated
        var item = await backend.GetItemAsync( itemWithDep, cancellationToken: CancellationToken.None );
        Assert.Null( item );
    }

    [Fact]
    public async Task OnMessageReceived_UnknownKind_LogsFailure()
    {
        await using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = _testBackendName } ),
            this._serviceProvider );

        var synchronizer = new TestCacheSynchronizer( backend, new CacheSynchronizerConfiguration() );
        await using var _ = synchronizer;

        var sourceId = Guid.NewGuid();
        var message = $"invalidate:unknown:{sourceId}:someKey";

        // Should not throw, but should log an error
        synchronizer.ProcessMessage( message );
        await synchronizer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        // Test passes if no exception is thrown
    }

    #endregion

    #region Own Message Filtering Tests

    [Fact]
    public async Task OnMessageReceived_OwnMessage_Ignored()
    {
        await using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = _testBackendName } ),
            this._serviceProvider );

        var synchronizer = new TestCacheSynchronizer( backend, new CacheSynchronizerConfiguration() );
        await using var _ = synchronizer;

        await backend.SetItemAsync( _testKey, new CacheItem( _testValue ), CancellationToken.None );

        // Use the same ID as the underlying backend to simulate own message
        var ownId = backend.Id;
        var message = $"invalidate:item:{ownId}:{_testKey}";

        synchronizer.ProcessMessage( message );
        await synchronizer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        // Item should still exist because message from self is ignored
        var item = await backend.GetItemAsync( _testKey, cancellationToken: CancellationToken.None );
        Assert.NotNull( item );
    }

    #endregion

    #region Malformed Message Tests

    [Fact]
    public async Task OnMessageReceived_InvalidGuid_HandledGracefully()
    {
        await using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = _testBackendName } ),
            this._serviceProvider );

        var synchronizer = new TestCacheSynchronizer( backend, new CacheSynchronizerConfiguration() );
        await using var _ = synchronizer;

        await backend.SetItemAsync( _testKey, new CacheItem( _testValue ), CancellationToken.None );

        // Invalid GUID
        const string message = $"invalidate:item:not-a-valid-guid:{_testKey}";

        // Should not throw
        synchronizer.ProcessMessage( message );
        await synchronizer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        // Item should still exist because the message was invalid
        var item = await backend.GetItemAsync( _testKey, cancellationToken: CancellationToken.None );
        Assert.NotNull( item );
    }

    [Fact]
    public async Task OnMessageReceived_TruncatedMessage_DoesNotThrow()
    {
        await using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = _testBackendName } ),
            this._serviceProvider );

        var synchronizer = new TestCacheSynchronizer( backend, new CacheSynchronizerConfiguration() );
        await using var _ = synchronizer;

        // Various truncated messages
        var truncatedMessages = new[] { "invalidate", "invalidate:", "invalidate:item", "invalidate:item:", $"invalidate:item:{Guid.NewGuid()}" };

        foreach ( var message in truncatedMessages )
        {
            // Should not throw
            synchronizer.ProcessMessage( message );
        }

        await synchronizer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();
    }

    [Fact]
    public async Task OnMessageReceived_EmptyMessage_DoesNotThrow()
    {
        await using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = _testBackendName } ),
            this._serviceProvider );

        var synchronizer = new TestCacheSynchronizer( backend, new CacheSynchronizerConfiguration() );
        await using var _ = synchronizer;

        synchronizer.ProcessMessage( "" );
        await synchronizer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();
    }

    #endregion

    #region Message Publishing Tests

    [Fact]
    public async Task RemoveItem_PublishesInvalidationMessage()
    {
        await using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = _testBackendName } ),
            this._serviceProvider );

        var synchronizer = new TestCacheSynchronizer( backend, new CacheSynchronizerConfiguration() );
        await using var _ = synchronizer;

        await backend.SetItemAsync( _testKey, new CacheItem( _testValue ), CancellationToken.None );

        await synchronizer.RemoveItemAsync( _testKey, CancellationToken.None );
        await synchronizer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        Assert.Single( synchronizer.SentMessages );
        var message = synchronizer.SentMessages[0];
        Assert.Contains( "item", message, StringComparison.Ordinal );
        Assert.Contains( _testKey, message, StringComparison.Ordinal );
        Assert.Contains( backend.Id.ToString(), message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task InvalidateDependency_PublishesInvalidationMessage()
    {
        await using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = _testBackendName } ),
            this._serviceProvider );

        var synchronizer = new TestCacheSynchronizer( backend, new CacheSynchronizerConfiguration() );
        await using var _ = synchronizer;

        await synchronizer.InvalidateDependencyAsync( _testDependency, CancellationToken.None );
        await synchronizer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        Assert.Single( synchronizer.SentMessages );
        var message = synchronizer.SentMessages[0];
        Assert.Contains( "dependency", message, StringComparison.Ordinal );
        Assert.Contains( _testDependency, message, StringComparison.Ordinal );
        Assert.Contains( backend.Id.ToString(), message, StringComparison.Ordinal );
    }

    #endregion

    #region Key with Colons Tests

    [Fact]
    public async Task OnMessageReceived_KeyWithColons_ParsedCorrectly()
    {
        await using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = _testBackendName } ),
            this._serviceProvider );

        var synchronizer = new TestCacheSynchronizer( backend, new CacheSynchronizerConfiguration() );
        await using var _ = synchronizer;

        // Key with colons
        const string keyWithColons = "namespace:class:method:param1:param2";
        await backend.SetItemAsync( keyWithColons, new CacheItem( _testValue ), CancellationToken.None );

        var sourceId = Guid.NewGuid();
        var message = $"invalidate:item:{sourceId}:{keyWithColons}";

        synchronizer.ProcessMessage( message );
        await synchronizer.WhenBackgroundTasksCompleted( CancellationToken.None ).WaitWithTimeoutAsync();

        // The item should be removed
        var item = await backend.GetItemAsync( keyWithColons, cancellationToken: CancellationToken.None );
        Assert.Null( item );
    }

    #endregion
}