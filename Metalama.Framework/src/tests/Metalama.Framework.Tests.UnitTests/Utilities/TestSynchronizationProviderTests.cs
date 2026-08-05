// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Testing.Hooks;
using Metalama.Testing.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

#pragma warning disable VSTHRD200 // Use "Async" suffix - test naming convention prefers descriptive names.

/// <summary>
/// Tests that <see cref="ITestSynchronizationProvider"/> can be consumed by an arbitrary component: that the
/// provider registered by <see cref="UnitTestClass"/> and exposed as <see cref="TestContext.SyncProvider"/> is
/// resolved from the global service provider, and that a synchronization point blocks until the test releases it.
/// </summary>
public sealed class TestSynchronizationProviderTests : UnitTestClass
{
    private const string _syncPointName = "ComponentUnderTest.Increment:BeforeIncrement";

    public TestSynchronizationProviderTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// Stands for a production component that declares a synchronization point. It resolves
    /// <see cref="ITestSynchronizationProvider"/> the way production code is expected to: untyped, because the
    /// interface cannot derive from <c>IGlobalService</c>, and optionally, because the service is absent in
    /// production even though <see cref="UnitTestClass"/> registers it in every test context.
    /// </summary>
    private sealed class ComponentUnderTest
    {
        private readonly ITestSynchronizationProvider? _syncProvider;

        public ComponentUnderTest( GlobalServiceProvider serviceProvider )
        {
            this._syncProvider = (ITestSynchronizationProvider?) serviceProvider.Underlying.GetService( typeof(ITestSynchronizationProvider) );
        }

        /// <summary>
        /// Gets the number of times the component completed its operation.
        /// </summary>
        public int Counter { get; private set; }

        /// <summary>
        /// Performs the operation, reaching the synchronization point before it takes effect.
        /// </summary>
        public async Task IncrementAsync( CancellationToken cancellationToken )
        {
            if ( this._syncProvider != null )
            {
                await this._syncProvider.SyncPointAsync( _syncPointName, cancellationToken );
            }

            this.Counter++;
        }

        /// <summary>
        /// Synchronous variant of <see cref="IncrementAsync"/>, standing for code that reaches a synchronization
        /// point while a lock is held.
        /// </summary>
        public void Increment( CancellationToken cancellationToken )
        {
            this._syncProvider?.SyncPoint( _syncPointName, cancellationToken );

            this.Counter++;
        }
    }

    [Fact]
    public async Task SyncPointIsSkippedWhenNotEnabled()
    {
        using var testContext = this.CreateTestContext();

        var component = new ComponentUnderTest( testContext.ServiceProvider.Global );

        // The provider is registered in every test context, but this test enabled no synchronization point, so
        // reaching one must not block.
        await component.IncrementAsync( testContext.CancellationToken );

        Assert.Equal( 1, component.Counter );
    }

    [Fact]
    public async Task AsyncSyncPointBlocksUntilReleased()
    {
        using var testContext = this.CreateTestContext();
        var syncProvider = testContext.SyncProvider;

        var component = new ComponentUnderTest( testContext.ServiceProvider.Global );

        syncProvider.EnableSyncPoint( _syncPointName );

        var incrementTask = Task.Run( () => component.IncrementAsync( testContext.CancellationToken ) );

        await syncProvider.WaitForSyncPointReachedAsync( _syncPointName, testContext.CancellationToken );

        // The component signals the synchronization point before waiting for its release, so it is now blocked
        // inside the synchronization point and cannot have incremented the counter.
        Assert.False( incrementTask.IsCompleted );
        Assert.Equal( 0, component.Counter );

        syncProvider.ReleaseSyncPoint( _syncPointName );

        await incrementTask;

        Assert.Equal( 1, component.Counter );
    }

    [Fact]
    public async Task SyncPointBlocksUntilReleased()
    {
        using var testContext = this.CreateTestContext();
        var syncProvider = testContext.SyncProvider;

        var component = new ComponentUnderTest( testContext.ServiceProvider.Global );

        syncProvider.EnableSyncPoint( _syncPointName );

        var incrementTask = Task.Run( () => component.Increment( testContext.CancellationToken ) );

        await syncProvider.WaitForSyncPointReachedAsync( _syncPointName, testContext.CancellationToken );

        Assert.False( incrementTask.IsCompleted );
        Assert.Equal( 0, component.Counter );

        syncProvider.ReleaseSyncPoint( _syncPointName );

        await incrementTask;

        Assert.Equal( 1, component.Counter );
    }
}
