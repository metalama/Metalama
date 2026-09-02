// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Backends;

/// <summary>
/// Tests of the expiration of <see cref="MemoryCachingBackend"/> that advance the clock instead of sleeping.
/// </summary>
/// <remarks>
/// The backend resolves its <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> and its work-item
/// dispatcher from the service provider of <see cref="FakeCachingServices"/>, so the whole chain is deterministic:
/// advancing the clock expires the entry, the post-eviction callback runs, the backend queues a work item, and the
/// test waits for that work item.
/// </remarks>
public sealed class MemoryCachingBackendExpirationTests
{
    private static readonly DateTimeOffset _origin = new( 2026, 1, 1, 0, 0, 0, TimeSpan.Zero );

    [Fact]
    public async Task AbsoluteExpiration_RaisesItemRemoved_WhenTheClockAdvances()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var cancellationTokenSource = new CancellationTokenSource();

        using var backend = CachingBackend.Create( b => b.Memory( new MemoryCachingBackendConfiguration() ), fakes.ServiceProvider );
        backend.Initialize();

        CacheItemRemovedEventArgs? removedArgs = null;
        backend.ItemRemoved += ( _, args ) => removedArgs = args;

        const string key = "expiring-key";

        backend.SetItem(
            key,
            new CacheItem( "value", configuration: new CacheItemConfiguration { AbsoluteExpiration = TimeSpan.FromMinutes( 5 ) } ) );

        Assert.NotNull( backend.GetItem( key ) );

        await fakes.AdvanceAsync( TimeSpan.FromMinutes( 6 ), cancellationTokenSource.Token );

        Assert.Null( backend.GetItem( key ) );
        Assert.NotNull( removedArgs );
        Assert.Equal( key, removedArgs.Key );
        Assert.Equal( CacheItemRemovedReason.Expired, removedArgs.RemovedReason );
    }

    [Fact]
    public async Task AbsoluteExpiration_KeepsTheItem_BeforeTheExpirationInstant()
    {
        using var fakes = new FakeCachingServices( _origin );
        using var cancellationTokenSource = new CancellationTokenSource();

        using var backend = CachingBackend.Create( b => b.Memory( new MemoryCachingBackendConfiguration() ), fakes.ServiceProvider );
        backend.Initialize();

        const string key = "expiring-key";

        backend.SetItem(
            key,
            new CacheItem( "value", configuration: new CacheItemConfiguration { AbsoluteExpiration = TimeSpan.FromMinutes( 5 ) } ) );

        await fakes.AdvanceAsync( TimeSpan.FromMinutes( 4 ), cancellationTokenSource.Token );

        var retrieved = backend.GetItem( key );
        Assert.NotNull( retrieved );
        Assert.Equal( "value", retrieved.Value );
    }
}
