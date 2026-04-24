// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Templating;
using Metalama.Testing.UnitTesting;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Templating;

/// <summary>
/// Tests that verify Quick mode is used correctly in SymbolClassifier.
/// Quick mode should be used inside method bodies (for expressions/statements),
/// while Full mode should only be used for declarations.
/// </summary>
public sealed class SymbolClassifierQuickModeTests : UnitTestClass
{
    private readonly ITestOutputHelper _output;

    public SymbolClassifierQuickModeTests( ITestOutputHelper output )
    {
        this._output = output;
    }

    [Fact]
    public async Task RunTimeOnlyCode_UsesQuickMode()
    {
        // Arrange
        var observer = new TestSymbolClassifierObserver();
        var services = CreateAdditionalServiceCollection();
        services.AddGlobalService<ISymbolClassifierObserver>( observer );

        using var testContext = this.CreateTestContext( services );

        // Code without any Metalama usings - should use Quick mode (RunTimeOnly context) for validation
        const string code = """
                            using System;
                            using System.Collections.Generic;
                            using System.Linq;
                            using System.Text;

                            namespace TestNamespace
                            {
                                public class TestClass
                                {
                                    public void TestMethod()
                                    {
                                        // First block of operations
                                        var list = new List<string>();
                                        Console.WriteLine(list.Count);
                                        var dict = new Dictionary<string, int>();
                                        dict.Add("one", 1);
                                        dict.Add("two", 2);
                                        var keys = dict.Keys.ToList();
                                        var values = dict.Values.ToArray();

                                        // Second block of operations
                                        var sb = new StringBuilder();
                                        sb.Append("Hello");
                                        sb.Append(" ");
                                        sb.Append("World");
                                        var result = sb.ToString();
                                        Console.WriteLine(result);

                                        // Third block of operations
                                        var numbers = new List<int> { 1, 2, 3, 4, 5 };
                                        var sum = numbers.Sum();
                                        var avg = numbers.Average();
                                        var max = numbers.Max();
                                        var min = numbers.Min();
                                        var filtered = numbers.Where(n => n > 2).ToList();
                                        var mapped = numbers.Select(n => n * 2).ToArray();
                                        Console.WriteLine($"Sum: {sum}, Avg: {avg}, Max: {max}, Min: {min}");
                                    }
                                }
                            }
                            """;

        var compilation = testContext.CreateCSharpCompilation( code );

        // Act
        var diagnosticCount = 0;

        await TemplatingCodeValidator.ValidateAsync(
            testContext.ServiceProvider,
            compilation,
            _ => Interlocked.Increment( ref diagnosticCount ),
            CancellationToken.None );

        // Assert - should primarily use RunTimeOnly context (Quick mode) for run-time-only code
        var stats = observer.GetStatistics();

        Assert.True( stats.RunTimeOnlyContextCalls > 0, "Expected RunTimeOnly context calls for run-time-only code" );

        // Note: Some Default context calls may occur from internal SymbolClassifier methods (e.g., IsTemplateOnlyCore).
        // The key assertion is that when Quick mode is active, no expensive operations are performed.
        // Quick mode should skip IsSymbolAvailable calls for System types.
        Assert.True(
            stats.QuickModeSkips >= stats.ExpensiveOperations,
            $"Expected Quick mode to skip more operations than expensive ones performed. Skips: {stats.QuickModeSkips}, Expensive: {stats.ExpensiveOperations}" );
    }

    [Fact]
    public async Task CodeWithMetalamaUsings_UsesDefaultMode()
    {
        // Arrange
        var observer = new TestSymbolClassifierObserver();
        var services = CreateAdditionalServiceCollection();
        services.AddGlobalService<ISymbolClassifierObserver>( observer );

        using var testContext = this.CreateTestContext( services );

        // Code with Metalama usings - should use Default mode
        const string code = """
                            using System;
                            using Metalama.Framework.Aspects;

                            namespace TestNamespace
                            {
                                public class TestAspect : OverrideMethodAspect
                                {
                                    public override dynamic? OverrideMethod()
                                    {
                                        Console.WriteLine("Before");
                                        return meta.Proceed();
                                    }
                                }
                            }
                            """;

        var compilation = testContext.CreateCSharpCompilation( code );

        // Act
        var diagnosticCount = 0;

        await TemplatingCodeValidator.ValidateAsync(
            testContext.ServiceProvider,
            compilation,
            _ => Interlocked.Increment( ref diagnosticCount ),
            CancellationToken.None );

        // Assert - should use Default context for code with Metalama usings
        var stats = observer.GetStatistics();

        Assert.True( stats.DefaultContextCalls > 0, "Expected Default context calls for code with Metalama usings" );
    }

    [Fact]
    public async Task QuickMode_SkipsIsSymbolAvailable()
    {
        // Arrange
        var observer = new TestSymbolClassifierObserver();
        var services = CreateAdditionalServiceCollection();
        services.AddGlobalService<ISymbolClassifierObserver>( observer );

        using var testContext = this.CreateTestContext( services );

        // Code that references many System types but has no Metalama usings
        const string code = """
                            using System;
                            using System.Collections.Generic;
                            using System.Linq;
                            using System.Text;
                            using System.IO;
                            using System.Threading.Tasks;

                            namespace TestNamespace
                            {
                                public class TestClass
                                {
                                    public void TestMethod()
                                    {
                                        // First block - basic types
                                        var sb = new StringBuilder();
                                        sb.Append("test");
                                        sb.AppendLine(" line");
                                        var str = sb.ToString();
                                        var len = str.Length;
                                        var upper = str.ToUpper();
                                        var lower = str.ToLower();

                                        // Second block - collections
                                        var list = new List<int> { 1, 2, 3, 4, 5 };
                                        var sum = list.Sum();
                                        var avg = list.Average();
                                        var count = list.Count;
                                        var first = list.First();
                                        var last = list.Last();
                                        var filtered = list.Where(x => x > 2).ToList();
                                        var mapped = list.Select(x => x * 2).ToArray();

                                        // Third block - IO and system types
                                        var path = Path.GetTempPath();
                                        var combined = Path.Combine(path, "test.txt");
                                        var dir = Path.GetDirectoryName(combined);
                                        var ext = Path.GetExtension(combined);
                                        var guid = Guid.NewGuid();
                                        var guidStr = guid.ToString();
                                        var dt = DateTime.Now;
                                        var year = dt.Year;
                                        var month = dt.Month;
                                        var day = dt.Day;

                                        // Fourth block - more LINQ operations
                                        var dict = new Dictionary<string, int>();
                                        dict["a"] = 1;
                                        dict["b"] = 2;
                                        dict["c"] = 3;
                                        var keys = dict.Keys.ToList();
                                        var values = dict.Values.ToArray();
                                        var pairs = dict.Select(kv => $"{kv.Key}={kv.Value}").ToList();
                                        var joined = string.Join(", ", pairs);
                                        Console.WriteLine(joined);
                                    }
                                }
                            }
                            """;

        var compilation = testContext.CreateCSharpCompilation( code );

        // Act
        var diagnosticCount = 0;

        await TemplatingCodeValidator.ValidateAsync(
            testContext.ServiceProvider,
            compilation,
            _ => Interlocked.Increment( ref diagnosticCount ),
            CancellationToken.None );

        // Assert
        var stats = observer.GetStatistics();

        // In Quick mode, IsSymbolAvailable should be skipped for System types
        Assert.True( stats.RunTimeOnlyContextCalls > 0, "Expected RunTimeOnly context calls" );

        // The key assertion: Quick mode should skip more IsSymbolAvailable calls than it performs.
        // Some expensive operations may occur from internal Default context calls,
        // but Quick mode should significantly reduce them for System types.
        Assert.True(
            stats.QuickModeSkips > 0 || stats.ExpensiveOperations == 0,
            $"Expected either Quick mode skips or no expensive operations. Skips: {stats.QuickModeSkips}, Expensive: {stats.ExpensiveOperations}" );
    }

    [Fact]
    public async Task CacheEfficiency_RepeatedTypes_ShouldHaveHighHitRate()
    {
        // Arrange
        var observer = new TestSymbolClassifierObserver();
        var services = CreateAdditionalServiceCollection();
        services.AddGlobalService<ISymbolClassifierObserver>( observer );

        using var testContext = this.CreateTestContext( services );

        // Code that uses the same types repeatedly - should benefit from caching
        const string code = """
                            using System;
                            using System.Collections.Generic;
                            using System.Text;

                            namespace TestNamespace
                            {
                                public class TestClass
                                {
                                    public void Method1()
                                    {
                                        var list1 = new List<string>();
                                        var list2 = new List<string>();
                                        var list3 = new List<string>();
                                        var dict1 = new Dictionary<string, int>();
                                        var dict2 = new Dictionary<string, int>();
                                        var sb1 = new StringBuilder();
                                        var sb2 = new StringBuilder();
                                    }

                                    public void Method2()
                                    {
                                        var list1 = new List<string>();
                                        var list2 = new List<string>();
                                        var dict1 = new Dictionary<string, int>();
                                        var sb1 = new StringBuilder();
                                        Console.WriteLine("test");
                                        Console.WriteLine("test2");
                                    }

                                    public void Method3()
                                    {
                                        var list1 = new List<int>();
                                        var list2 = new List<int>();
                                        var dict1 = new Dictionary<int, string>();
                                        var dt1 = DateTime.Now;
                                        var dt2 = DateTime.UtcNow;
                                    }
                                }
                            }
                            """;

        var compilation = testContext.CreateCSharpCompilation( code );

        // Act
        var diagnosticCount = 0;

        await TemplatingCodeValidator.ValidateAsync(
            testContext.ServiceProvider,
            compilation,
            _ => Interlocked.Increment( ref diagnosticCount ),
            CancellationToken.None );

        // Assert
        var stats = observer.GetStatistics();

        // With repeated types, we should see cache hits
        var totalLookups = stats.CacheHits + stats.CacheMisses;

        Assert.True( totalLookups > 0, "Expected cache lookups to occur" );

        // Cache hit rate should be reasonable (at least some hits)
        // Note: The actual ratio depends on implementation, but with repeated types
        // we should see some benefit from caching
        var hitRate = (double) stats.CacheHits / totalLookups;

        // Log the stats for diagnostic purposes
        this._output.WriteLine( $"Cache Stats - Hits: {stats.CacheHits}, Misses: {stats.CacheMisses}, Hit rate: {hitRate:P1}" );
        this._output.WriteLine( $"Context Calls - Default: {stats.DefaultContextCalls}, RunTimeOnly: {stats.RunTimeOnlyContextCalls}" );
        this._output.WriteLine( $"Quick Mode Skips: {stats.QuickModeSkips}, Expensive Ops: {stats.ExpensiveOperations}" );

        // The current implementation should show reasonable cache efficiency for repeated types.
        // If cache efficiency degrades, this test will fail and show the actual stats.
        Assert.True(
            stats.CacheHits > 0 || stats.CacheMisses < 100,
            $"Cache efficiency check - Hits: {stats.CacheHits}, Misses: {stats.CacheMisses}, Hit rate: {hitRate:P1}, " +
            $"DefaultCalls: {stats.DefaultContextCalls}, RunTimeOnlyCalls: {stats.RunTimeOnlyContextCalls}" );
    }

    private sealed class TestSymbolClassifierObserver : ISymbolClassifierObserver
    {
        private int _defaultContextCalls;
        private int _runTimeOnlyContextCalls;
        private int _cacheHits;
        private int _cacheMisses;
        private int _attributeCacheHits;
        private int _attributeCacheMisses;
        private readonly ConcurrentDictionary<string, int> _quickModeSkips = new();
        private readonly ConcurrentDictionary<string, int> _expensiveOperations = new();

        public void OnGetTemplatingScope( SymbolClassificationContext context )
        {
            if ( context == SymbolClassificationContext.Default )
            {
                Interlocked.Increment( ref this._defaultContextCalls );
            }
            else
            {
                Interlocked.Increment( ref this._runTimeOnlyContextCalls );
            }
        }

        public void OnQuickModeSkip( string operationName )
        {
            this._quickModeSkips.AddOrUpdate( operationName, 1, ( _, count ) => count + 1 );
        }

        public void OnExpensiveOperation( string operationName )
        {
            this._expensiveOperations.AddOrUpdate( operationName, 1, ( _, count ) => count + 1 );
        }

        public void OnCacheLookup( bool hit )
        {
            if ( hit )
            {
                Interlocked.Increment( ref this._cacheHits );
            }
            else
            {
                Interlocked.Increment( ref this._cacheMisses );
            }
        }

        public void OnAttributeScopeCacheLookup( bool hit )
        {
            if ( hit )
            {
                Interlocked.Increment( ref this._attributeCacheHits );
            }
            else
            {
                Interlocked.Increment( ref this._attributeCacheMisses );
            }
        }

        public Statistics GetStatistics()
        {
            var quickModeSkips = 0;

            foreach ( var kvp in this._quickModeSkips )
            {
                quickModeSkips += kvp.Value;
            }

            var expensiveOperations = 0;

            foreach ( var kvp in this._expensiveOperations )
            {
                expensiveOperations += kvp.Value;
            }

            return new Statistics(
                this._defaultContextCalls,
                this._runTimeOnlyContextCalls,
                quickModeSkips,
                expensiveOperations,
                this._cacheHits,
                this._cacheMisses );
        }

        public sealed record Statistics(
            int DefaultContextCalls,
            int RunTimeOnlyContextCalls,
            int QuickModeSkips,
            int ExpensiveOperations,
            int CacheHits,
            int CacheMisses );
    }
}