// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace;
using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.TestHelpers;

public abstract class BaseCachingTests : ICachingExceptionObserver
{
    protected BaseCachingTests( ITestOutputHelper testOutputHelper )
    {
        this.TestOutputHelper = testOutputHelper;
        var serviceCollection = new ServiceCollection();

        // ReSharper disable once VirtualMemberCallInConstructor
        AddLogging( serviceCollection, testOutputHelper );
        serviceCollection.AddSingleton<ICachingExceptionObserver>( this );

        // ReSharper disable once VirtualMemberCallInConstructor
        this.AddServices( serviceCollection );

        this.ServiceProvider = serviceCollection.BuildServiceProvider();
        CachingService.Default = CachingService.CreateUninitialized( this.ServiceProvider );
    }

    protected BackgroundTaskSchedulerObserver BackgroundTaskSchedulerObserver { get; } = new();

    protected virtual void AddServices( ServiceCollection serviceCollection )
    {
        serviceCollection.AddSingleton<IBackgroundTaskSchedulerObserver>( this.BackgroundTaskSchedulerObserver );
    }

    private static void AddLogging( IServiceCollection serviceCollection, ITestOutputHelper testOutputHelper )
    {
        serviceCollection.AddSingleton<IFlashtraceLoggerFactory>( new XUnitFlashtraceLoggerFactory( testOutputHelper ) );
    }

    protected ServiceProvider ServiceProvider { get; }

    protected ITestOutputHelper TestOutputHelper { get; }

    private static void ResetCachingServices()
    {
        CachingService.Default.Dispose();
        var uninitialized = CachingService.CreateUninitialized();
        CachingService.Default = uninitialized;
    }

    protected CachingTestContext<T> InitializeTest<T>(
        string name,
        T backend,
        Action<CachingTestBuilder>? buildTest = null,
        bool passServiceProvider = true )
        where T : CachingBackend
    {
        ResetCachingServices();

        CachingService.Default = CachingService.Create(
            b =>
            {
                b.WithBackend( x => x.Specific( backend ) );

                var testBuilder = new CachingTestBuilder( b );
                buildTest?.Invoke( testBuilder );
                b.AddProfile( new CachingProfile( name ), true );
            },
            passServiceProvider ? this.ServiceProvider : null );

        return new CachingTestContext<T>( backend );
    }

    protected CachingTestContext<CachingBackend> InitializeTest(
        string name,
        Action<CachingTestBuilder>? buildTest = null,
        bool passServiceProvider = true )
    {
        var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration() { DebugName = name } ),
            passServiceProvider ? this.ServiceProvider : null );

        return this.InitializeTest( name, backend, buildTest, passServiceProvider );
    }

    protected CachingTestContext<TestingCacheBackend> InitializeTestWithTestingBackend(
        string name,
        Action<CachingTestBuilder>? buildTest = null )
    {
        var backend = new TestingCacheBackend( "test-" + name, this.ServiceProvider );

        return this.InitializeTest( name, backend, buildTest );
    }

    public virtual void OnException( CachingExceptionInfo exceptionInfo ) => exceptionInfo.Rethrow = true;
}