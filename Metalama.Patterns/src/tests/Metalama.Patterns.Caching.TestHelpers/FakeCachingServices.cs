// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Metalama.Patterns.Caching.TestHelpers;

/// <summary>
/// The service provider through which a test class substitutes the clock, the work-item dispatcher and the memory
/// cache of a caching backend.
/// </summary>
/// <remarks>
/// <para>
/// Substitution is opt-in per test class. A test that does not depend on a duration keeps running against the real
/// thread pool, the real clock and <see cref="MemoryCache"/>. Pass <see cref="ServiceProvider"/> to the backend under
/// test, advance the clock with <see cref="AdvanceAsync"/>, and assert.
/// </para>
/// <para>
/// The <see cref="IMemoryCache"/> is registered as a single instance, as <c>AddMemoryCache</c> does. Two backends
/// that resolve it from this service provider therefore share one store, and each of them prefixes its keys with an
/// identifier of its own, so they keep separate items and separate dependencies.
/// </para>
/// </remarks>
public sealed class FakeCachingServices : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FakeCachingServices"/> class.
    /// </summary>
    /// <param name="startTime">The instant at which the clock starts, or <see langword="null"/> for the default of <see cref="FakeTimeProvider"/>.</param>
    /// <param name="configureServices">An action that adds further services, or <see langword="null"/>.</param>
    public FakeCachingServices( DateTimeOffset? startTime = null, Action<ServiceCollection>? configureServices = null )
    {
        this.TimeProvider = startTime == null ? new FakeTimeProvider() : new FakeTimeProvider( startTime.Value );
        this.WorkItemDispatcher = new TestWorkItemDispatcher();
        this.MemoryCache = new FakeMemoryCache( this.TimeProvider );

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>( this.TimeProvider );
        services.AddSingleton<IWorkItemDispatcher>( this.WorkItemDispatcher );
        services.AddSingleton<IMemoryCache>( this.MemoryCache );
        configureServices?.Invoke( services );

        this.ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets the clock of the backends built on <see cref="ServiceProvider"/>.
    /// </summary>
    public FakeTimeProvider TimeProvider { get; }

    /// <summary>
    /// Gets the work-item dispatcher of the backends built on <see cref="ServiceProvider"/>.
    /// </summary>
    public TestWorkItemDispatcher WorkItemDispatcher { get; }

    /// <summary>
    /// Gets the memory cache of the backends built on <see cref="ServiceProvider"/>.
    /// </summary>
    public FakeMemoryCache MemoryCache { get; }

    /// <summary>
    /// Gets the service provider to pass to the backend under test.
    /// </summary>
    public ServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Advances the clock, then waits for the work items that the advance has queued.
    /// </summary>
    /// <param name="delta">The amount by which the clock advances.</param>
    /// <param name="cancellationToken">A token that cancels the wait.</param>
    public Task AdvanceAsync( TimeSpan delta, CancellationToken cancellationToken = default )
    {
        this.TimeProvider.Advance( delta );

        return this.WorkItemDispatcher.WhenPendingWorkItemsCompletedAsync( cancellationToken );
    }

    /// <summary>
    /// Returns a <see cref="Task"/> that completes when no work item is pending.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the wait.</param>
    public Task WhenPendingWorkItemsCompletedAsync( CancellationToken cancellationToken = default )
        => this.WorkItemDispatcher.WhenPendingWorkItemsCompletedAsync( cancellationToken );

    /// <inheritdoc />
    public void Dispose()
    {
        this.ServiceProvider.Dispose();
        this.MemoryCache.Dispose();
    }
}
