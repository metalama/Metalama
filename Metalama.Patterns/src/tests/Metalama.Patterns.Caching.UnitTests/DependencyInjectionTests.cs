// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.TestHelpers;
using Metalama.Patterns.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests;

public sealed class DependencyInjectionTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DependencyInjectionTests( ITestOutputHelper testOutputHelper )
    {
        this._testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task TestDependencyInjection()
    {
        TestingCacheBackend? backend = null;
        ServiceCollection serviceCollection = new();
        serviceCollection.AddLogging( logging => logging.AddXUnitLogger( this._testOutputHelper ).SetMinimumLevel( LogLevel.Debug ) );
        serviceCollection.AddMetalamaCaching( b => b.WithBackend( backend = new TestingCacheBackend( "test", b.ServiceProvider ) ) );
        serviceCollection.AddSingleton<C>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        await using var initializer = serviceProvider.GetRequiredService<ICachingService>();
        await initializer.InitializeAsync();

        var c = (C) serviceProvider.GetService( typeof(C) )!;
        _ = c.Method();

        Assert.NotNull( backend!.LastCachedItem );
        Assert.Equal( "DependencyInjection!", backend.LastCachedItem.Value );

        var observer = serviceProvider.GetRequiredService<LogObserver>();
        Assert.NotEmpty( observer.Lines );
        Assert.StartsWith( "Debug Caching.Metalama.Patterns.Caching.Tests.DependencyInjectionTests+C:", observer.Lines[0], StringComparison.Ordinal );
    }

    private sealed class C
    {
        [Cache( UseDependencyInjection = true )]

        // ReSharper disable once MemberCanBeMadeStatic.Local
#pragma warning disable CA1822
        public string Method() => "DependencyInjection!";
#pragma warning restore CA1822
    }
}