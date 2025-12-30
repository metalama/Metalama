// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating;

public sealed class ClassifyingCompilationContextCacheTests : UnitTestClass
{
    [Fact]
    public async Task CacheHitsOnRepeatedValidation()
    {
        // Arrange
        var observer = new TestWeakCacheObserver();
        var services = CreateAdditionalServiceCollection();
        services.AddGlobalService<IWeakCacheObserver>( observer );

        using var testContext = this.CreateTestContext( services );

        const string code = """
                            using System;

                            class C
                            {
                                void M1() { Console.WriteLine(); }
                                void M2() { Console.WriteLine(); }
                                void M3() { Console.WriteLine(); }
                            }
                            """;

        var compilation = testContext.CreateCSharpCompilation( code );

        // Act - validate the same compilation multiple times
        var diagnosticCount = 0;

        await TemplatingCodeValidator.ValidateAsync(
            testContext.ServiceProvider,
            compilation,
            _ => Interlocked.Increment( ref diagnosticCount ),
            CancellationToken.None );

        // Assert - should have 1 miss (first access) and 0 hits for ClassifyingCompilationContext
        var stats = observer.GetStatistics( ClassifyingCompilationContextFactory.CacheName );

        Assert.Equal( 1, stats.Misses );
        Assert.Equal( 0, stats.Hits ); // ValidateAsync calls GetInstance once at the beginning
    }

    [Fact]
    public async Task CacheHitsOnSecondValidation()
    {
        // Arrange
        var observer = new TestWeakCacheObserver();
        var services = CreateAdditionalServiceCollection();
        services.AddGlobalService<IWeakCacheObserver>( observer );

        using var testContext = this.CreateTestContext( services );

        const string code = """
                            using System;

                            class C
                            {
                                void M() { Console.WriteLine(); }
                            }
                            """;

        var compilation = testContext.CreateCSharpCompilation( code );

        // Act - validate the same compilation twice
        var diagnosticCount = 0;

        await TemplatingCodeValidator.ValidateAsync(
            testContext.ServiceProvider,
            compilation,
            _ => Interlocked.Increment( ref diagnosticCount ),
            CancellationToken.None );

        await TemplatingCodeValidator.ValidateAsync(
            testContext.ServiceProvider,
            compilation,
            _ => Interlocked.Increment( ref diagnosticCount ),
            CancellationToken.None );

        // Assert - should have 1 miss (first validation) and 1 hit (second validation)
        var stats = observer.GetStatistics( ClassifyingCompilationContextFactory.CacheName );

        Assert.Equal( 1, stats.Misses );
        Assert.Equal( 1, stats.Hits );
    }

    [Fact]
    public async Task CacheSurvivesGarbageCollection()
    {
        // Arrange
        var observer = new TestWeakCacheObserver();
        var services = CreateAdditionalServiceCollection();
        services.AddGlobalService<IWeakCacheObserver>( observer );

        using var testContext = this.CreateTestContext( services );

        const string code = """
                            using System;

                            class C
                            {
                                void M() { Console.WriteLine(); }
                            }
                            """;

        var compilation = testContext.CreateCSharpCompilation( code );

        // Act - validate, force GC, then validate again
        var diagnosticCount = 0;

        await TemplatingCodeValidator.ValidateAsync(
            testContext.ServiceProvider,
            compilation,
            _ => Interlocked.Increment( ref diagnosticCount ),
            CancellationToken.None );

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        await TemplatingCodeValidator.ValidateAsync(
            testContext.ServiceProvider,
            compilation,
            _ => Interlocked.Increment( ref diagnosticCount ),
            CancellationToken.None );

        // Assert - cache should survive GC since the Compilation key is still referenced
        var stats = observer.GetStatistics( ClassifyingCompilationContextFactory.CacheName );

        Assert.Equal( 1, stats.Misses );
        Assert.Equal( 1, stats.Hits );
    }

    [Fact]
    public void FactoryReturnsSameInstanceForSameCompilation()
    {
        // Arrange
        var observer = new TestWeakCacheObserver();
        var services = CreateAdditionalServiceCollection();
        services.AddGlobalService<IWeakCacheObserver>( observer );

        using var testContext = this.CreateTestContext( services );

        const string code = "class C { }";
        var compilation = testContext.CreateCSharpCompilation( code );

        var factory = testContext.ServiceProvider.GetRequiredService<ClassifyingCompilationContextFactory>();

        // Act
        var context1 = factory.GetInstance( compilation );
        var context2 = factory.GetInstance( compilation );

        // Assert - should return the same instance
        Assert.Same( context1, context2 );

        var stats = observer.GetStatistics( ClassifyingCompilationContextFactory.CacheName );
        Assert.Equal( 1, stats.Misses );
        Assert.Equal( 1, stats.Hits );
    }

    private sealed class TestWeakCacheObserver : IWeakCacheObserver
    {
        private readonly ConcurrentDictionary<string, CacheStatistics> _statistics = new();

        public void OnCacheHit( string cacheName )
        {
            var stats = this._statistics.GetOrAdd( cacheName, _ => new CacheStatistics() );
            Interlocked.Increment( ref stats.Hits );
        }

        public void OnCacheMiss( string cacheName )
        {
            var stats = this._statistics.GetOrAdd( cacheName, _ => new CacheStatistics() );
            Interlocked.Increment( ref stats.Misses );
        }

        public CacheStatistics GetStatistics( string cacheName )
        {
            return this._statistics.GetOrAdd( cacheName, _ => new CacheStatistics() );
        }

        public sealed class CacheStatistics
        {
            public int Hits;
            public int Misses;
        }
    }
}
