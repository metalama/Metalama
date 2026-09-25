// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.Tests.Implementation;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Backends;

/// <summary>
/// Tests that the operations of <see cref="MemoryCachingBackend"/> do not deadlock when they run concurrently. The tests
/// force a specific interleaving through the synchronization points of the backend.
/// </summary>
public sealed class MemoryCachingBackendConcurrencyTests
{
    private const string _removalSyncPointName = "MemoryCachingBackend.RemoveItemImpl:ItemLocked";
    private const string _invalidationSyncPointName = "MemoryCachingBackend.InvalidateDependencyImpl:DependencyLocked";

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// Reproduces metalama/Metalama#2066. The removal of an item and the invalidation of one of its dependencies must
    /// both complete when they run concurrently.
    /// </summary>
    /// <remarks>
    /// The removal thread pauses while it holds the monitor of the item. The invalidation thread pauses after it has
    /// acquired the monitor of the dependency. Before the fix, each thread then waited for the monitor that the other
    /// thread held. <paramref name="removalFirst"/> determines which thread reaches its synchronization point first.
    /// </remarks>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void RemoveItem_ConcurrentWithInvalidateDependency_DoesNotDeadlock( bool removalFirst )
    {
        using var syncProvider = new TestSynchronizationProvider();

        using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = "test" } ),
            syncProvider );

        backend.Initialize();

        const string key = "item";
        const string dependency = "dependency";

        backend.SetItem( key, new CacheItem( "value", [dependency] ) );

        var removalSyncPoint = syncProvider.Arm( _removalSyncPointName );
        var invalidationSyncPoint = syncProvider.Arm( _invalidationSyncPointName );

        var removalThread = new Thread( () => backend.RemoveItem( key ) ) { IsBackground = true, Name = "RemoveItem" };

        var invalidationThread = new Thread( () => backend.InvalidateDependency( dependency ) )
        {
            IsBackground = true, Name = "InvalidateDependency"
        };

        if ( removalFirst )
        {
            removalThread.Start();
            Assert.True( removalSyncPoint.WaitUntilReached( _timeout ), "The removal thread did not reach its synchronization point." );

            invalidationThread.Start();
            Assert.True( invalidationSyncPoint.WaitUntilReached( _timeout ), "The invalidation thread did not reach its synchronization point." );
        }
        else
        {
            invalidationThread.Start();
            Assert.True( invalidationSyncPoint.WaitUntilReached( _timeout ), "The invalidation thread did not reach its synchronization point." );

            removalThread.Start();
            Assert.True( removalSyncPoint.WaitUntilReached( _timeout ), "The removal thread did not reach its synchronization point." );
        }

        removalSyncPoint.Release();
        invalidationSyncPoint.Release();

        Assert.True( removalThread.Join( _timeout ), "The removal thread did not complete." );
        Assert.True( invalidationThread.Join( _timeout ), "The invalidation thread did not complete." );

        Assert.Null( backend.GetItem( key ) );
        Assert.False( backend.ContainsDependency( dependency ) );
    }
}
