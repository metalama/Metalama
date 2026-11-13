// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests that a cached method returns the new data on the next call when its data changes and the method is invalidated
/// while its value is being computed.
/// </summary>
/// <remarks>
/// <para>
/// After a cache miss, the caching frontend calls the method and then stores the value that the method returned. An
/// invalidation that runs between these two steps finds neither the cache item nor the registration of the item under
/// its dependency. It therefore removes nothing, and no component of the caching service records that the invalidation
/// happened. The value that the method computed from the old data is stored afterwards, and the following calls return
/// it until the item is invalidated again, removed or expired. The invalidation methods do not acquire the lock of the
/// locking strategy, so the locking strategy does not prevent this sequence.
/// </para>
/// <para>
/// Each test blocks a computation of the method after the method has read its data. While the computation is blocked,
/// the test changes the data and invalidates the method. The test then releases the computation, waits until the value
/// is stored, and calls the method again. The computation blocks in the method itself, so the schedule does not depend
/// on the order in which the backend calls the memory cache.
/// </para>
/// <para>
/// The tests assert the behavior that a caller expects. They fail as long as the caching service stores a value that
/// was computed before an invalidation that ran during the computation.
/// </para>
/// </remarks>
public sealed partial class CachingFrontendInvalidationRaceTests : BaseCachingTests
{
    /// <summary>
    /// The name of the caching profile of the cached methods of <see cref="VersionedDataSource"/>.
    /// </summary>
    private const string _profileName = "CachingFrontendInvalidationRaceTests";

    /// <summary>
    /// The dependency that every computation of <see cref="VersionedDataSource"/> adds to the current caching context.
    /// </summary>
    private const string _dependencyKey = "VersionedData";

    /// <summary>
    /// The failure message of the tests in which the next call returns the value that the first call computed from
    /// version 1 of the data instead of version 2.
    /// </summary>
    private const string _staleFirstValueMessage =
        "The second call did not return version 2 of the data. The value that the first call computed from version 1 was stored "
        + "after the invalidation that ran during its computation.";

    /// <summary>
    /// The maximum time that a test waits for a thread, a computation or a work item. It only detects a failure, such as
    /// a deadlock.
    /// </summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// The work-item dispatcher of the caching service and of the backend.
    /// </summary>
    /// <remarks>
    /// The backend raises its events through this dispatcher, and the automatic reload runs its refreshes through it.
    /// A test waits for the completion of these work items instead of waiting for a duration. The field initializer
    /// runs before the base constructor, so the field is set when the base constructor calls <see cref="AddServices"/>.
    /// </remarks>
    private readonly TestWorkItemDispatcher _workItemDispatcher = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingFrontendInvalidationRaceTests"/> class.
    /// </summary>
    /// <param name="testOutputHelper">The output of the test.</param>
    public CachingFrontendInvalidationRaceTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }

    /// <summary>
    /// Registers the services of the base class and <see cref="_workItemDispatcher"/> as the work-item dispatcher.
    /// </summary>
    /// <param name="serviceCollection">The service collection of the test.</param>
    protected override void AddServices( ServiceCollection serviceCollection )
    {
        base.AddServices( serviceCollection );
        serviceCollection.AddSingleton<IWorkItemDispatcher>( this._workItemDispatcher );
    }

    /// <summary>
    /// A synchronous cached method whose dependency is invalidated while its value is being computed must be computed
    /// again on the next call and must return the new data.
    /// </summary>
    /// <remarks>
    /// The first computation reads version 1 of the data and blocks. The test changes the data to version 2 and
    /// invalidates the dependency. The first value is not stored yet, so the backend has no registration of the item
    /// under the dependency, and the invalidation removes nothing. The first call then stores version 1 with its
    /// dependency, and the second call returns version 1 from the cache.
    /// </remarks>
    [Fact( Skip = "Not fixed: a value computed before an invalidation can be stored after it, because no layer of the caching stack orders the store after the invalidation. A fix needs a design decision, such as invalidation versions checked by the frontend." )]
    public void CachedMethod_DependencyInvalidatedDuringComputation_IsRecomputedOnNextCall()
    {
        this.RunInvalidationDuringSynchronousComputation( _ => CachingService.Default.Invalidate( _dependencyKey ) );
    }

    /// <summary>
    /// A synchronous cached method whose call is invalidated by its method key while its value is being computed must
    /// be computed again on the next call and must return the new data.
    /// </summary>
    /// <remarks>
    /// The invalidation builds the key of the call from the method, the instance and the arguments, and removes the
    /// cache item under this key. The first value is not stored yet, so the removal finds nothing. The first call then
    /// stores version 1, and the second call returns version 1 from the cache. The profile uses the default locking
    /// strategy. The invalidation does not acquire the lock of the key with any locking strategy.
    /// </remarks>
    [Fact( Skip = "Not fixed: a value computed before an invalidation can be stored after it, because no layer of the caching stack orders the store after the invalidation. A fix needs a design decision, such as invalidation versions checked by the frontend." )]
    public void CachedMethod_InvalidatedByMethodKeyDuringComputation_IsRecomputedOnNextCall()
    {
        this.RunInvalidationDuringSynchronousComputation( source => CachingService.Default.Invalidate( source.GetVersion ) );
    }

    /// <summary>
    /// An asynchronous cached method whose dependency is invalidated while its value is being computed must be computed
    /// again on the next call and must return the new data.
    /// </summary>
    /// <remarks>
    /// The schedule is the same as in <see cref="CachedMethod_DependencyInvalidatedDuringComputation_IsRecomputedOnNextCall"/>.
    /// The first call runs on a dedicated thread, which waits synchronously for the task of the cached method. The
    /// invalidation and the second call use the asynchronous methods of the caching service.
    /// </remarks>
    [Fact( Skip = "Not fixed: a value computed before an invalidation can be stored after it, because no layer of the caching stack orders the store after the invalidation. A fix needs a design decision, such as invalidation versions checked by the frontend." )]
    public async Task CachedMethodAsync_DependencyInvalidatedDuringComputation_IsRecomputedOnNextCall()
    {
        using var context = this.InitializeTest( _profileName );
        using var source = new VersionedDataSource( blockedComputation: 1 );
        using var cancellationTokenSource = new CancellationTokenSource( _timeout );
        var cancellationToken = cancellationTokenSource.Token;

        var firstResult = 0;
        var firstCall = ConcurrencyTestWorker.Start( "FirstCall", () => firstResult = GetVersionSynchronously( source, cancellationToken ) );

        try
        {
            Assert.True(
                await source.WaitUntilComputationBlockedAsync( _timeout, cancellationToken ),
                "The first call did not reach the point where its computation blocks." );

            AssertValue( 1, source.BlockedComputationVersion, "The blocked computation did not read version 1 of the data." );

            source.Version = 2;
            await CachingService.Default.InvalidateAsync( _dependencyKey, cancellationToken );
        }
        finally
        {
            source.ReleaseComputation();
        }

        AssertCompleted( firstCall );
        AssertValue( 1, firstResult, "The first call did not return the value that it computed from version 1 of the data." );

        var secondResult = await source.GetVersionAsync( cancellationToken );

        this.TestOutputHelper.WriteLine(
            $"First call: {firstResult}. Second call: {secondResult}. Number of computations: {source.ComputationCount}." );

        AssertValue( 2, secondResult, _staleFirstValueMessage );
    }

    /// <summary>
    /// A cached method with automatic reload whose dependency is invalidated again while the refresh that follows a
    /// first invalidation is being computed must return the newest data on the next call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first call stores version 1. The test changes the data to version 2 and invalidates the dependency. The
    /// invalidation removes the cache item, and the automatic reload starts a refresh, which reads version 2 and blocks.
    /// The test changes the data to version 3 and invalidates the dependency again. This second invalidation finds
    /// neither the cache item nor the registration of the item under the dependency, so it removes nothing and starts no
    /// other refresh. The refresh then stores version 2, and no component of the caching service computes the value
    /// again.
    /// </para>
    /// <para>
    /// The refresh runs as a work item of <see cref="_workItemDispatcher"/>. The test waits for the completion of all
    /// work items before the second call, so that the second call does not run concurrently with the refresh.
    /// </para>
    /// </remarks>
    [Fact( Skip = "Not fixed: a value computed before an invalidation can be stored after it, because no layer of the caching stack orders the store after the invalidation. A fix needs a design decision, such as invalidation versions checked by the frontend." )]
    public void AutoReloadedMethod_DependencyInvalidatedDuringRefresh_ReturnsNewDataOnNextCall()
    {
        using var context = this.InitializeTest( _profileName );
        using var source = new VersionedDataSource( blockedComputation: 2 );
        using var cancellationTokenSource = new CancellationTokenSource( _timeout );

        AssertValue( 1, source.GetVersionWithAutoReload(), "The first call did not return version 1 of the data." );

        try
        {
            source.Version = 2;
            CachingService.Default.Invalidate( _dependencyKey );

            Assert.True(
                source.WaitUntilComputationBlocked( _timeout ),
                "The first invalidation did not start a refresh that reached the point where its computation blocks." );

            AssertValue( 2, source.BlockedComputationVersion, "The refresh did not read version 2 of the data." );

            source.Version = 3;
            CachingService.Default.Invalidate( _dependencyKey );
        }
        finally
        {
            source.ReleaseComputation();
        }

        try
        {
            this._workItemDispatcher.WaitForPendingWorkItems( cancellationTokenSource.Token );
        }
        catch ( OperationCanceledException )
        {
            Assert.Fail( "The refresh did not complete after its computation was released." );
        }

        var secondResult = source.GetVersionWithAutoReload();

        this.TestOutputHelper.WriteLine( $"Second call: {secondResult}. Number of computations: {source.ComputationCount}." );

        AssertValue(
            3,
            secondResult,
            "The second call did not return version 3 of the data. The refresh stored the value that it computed from version 2 "
            + "after the invalidation that ran during its computation, and no further refresh was started." );
    }

    /// <summary>
    /// Runs the scenario of the synchronous tests. A first call of <see cref="VersionedDataSource.GetVersion"/> blocks
    /// after it has read version 1 of the data. While the computation is blocked, the test changes the data to version 2
    /// and runs <paramref name="invalidate"/>. After the first call has completed, the next call must return version 2.
    /// </summary>
    /// <param name="invalidate">The invalidation that runs while the first computation is blocked. It receives the data source.</param>
    private void RunInvalidationDuringSynchronousComputation( Action<VersionedDataSource> invalidate )
    {
        using var context = this.InitializeTest( _profileName );
        using var source = new VersionedDataSource( blockedComputation: 1 );

        var firstResult = 0;
        var firstCall = ConcurrencyTestWorker.Start( "FirstCall", () => firstResult = source.GetVersion() );

        try
        {
            Assert.True( source.WaitUntilComputationBlocked( _timeout ), "The first call did not reach the point where its computation blocks." );

            AssertValue( 1, source.BlockedComputationVersion, "The blocked computation did not read version 1 of the data." );

            source.Version = 2;
            invalidate( source );
        }
        finally
        {
            source.ReleaseComputation();
        }

        AssertCompleted( firstCall );
        AssertValue( 1, firstResult, "The first call did not return the value that it computed from version 1 of the data." );

        var secondResult = source.GetVersion();

        this.TestOutputHelper.WriteLine(
            $"First call: {firstResult}. Second call: {secondResult}. Number of computations: {source.ComputationCount}." );

        AssertValue( 2, secondResult, _staleFirstValueMessage );
    }

    /// <summary>
    /// Calls <see cref="VersionedDataSource.GetVersionAsync"/> and blocks the calling thread until the returned task
    /// completes.
    /// </summary>
    /// <remarks>
    /// The method runs on the thread of a <see cref="ConcurrencyTestWorker"/>, which has no synchronization context. The
    /// continuations of the cached method therefore run on the thread pool, and the blocked thread does not prevent them
    /// from running.
    /// </remarks>
    /// <param name="source">The data source.</param>
    /// <param name="cancellationToken">The token passed to the cached method.</param>
    /// <returns>The value that the cached method returned.</returns>
    private static int GetVersionSynchronously( VersionedDataSource source, CancellationToken cancellationToken )
        => source.GetVersionAsync( cancellationToken ).GetAwaiter().GetResult();

    /// <summary>
    /// Asserts that a worker completes within <see cref="_timeout"/> and that its action threw no exception.
    /// </summary>
    /// <param name="worker">The worker.</param>
    private static void AssertCompleted( ConcurrencyTestWorker worker )
    {
        Assert.True( worker.Join( _timeout ), $"The thread '{worker.Name}' did not complete after its computation was released." );

        if ( worker.Exception != null )
        {
            Assert.Fail( $"The thread '{worker.Name}' threw an exception: {worker.Exception}" );
        }
    }

    /// <summary>
    /// Fails the test when a value differs from the expected value. The failure message states the purpose of the
    /// check, the expected value and the actual value.
    /// </summary>
    /// <remarks>
    /// The <c>Assert.Equal</c> methods of xUnit do not accept a message. The message is required to report which step of
    /// the scenario produced the value.
    /// </remarks>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <param name="message">The statement that describes the failed check.</param>
    private static void AssertValue( int expected, int actual, string message )
    {
        if ( actual != expected )
        {
            Assert.Fail( $"{message} Expected: {expected}. Actual: {actual}." );
        }
    }
}
