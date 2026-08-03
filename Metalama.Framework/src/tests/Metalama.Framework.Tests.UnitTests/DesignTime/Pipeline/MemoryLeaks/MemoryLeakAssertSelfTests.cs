// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Tests the memory-leak test infrastructure itself.
/// </summary>
/// <remarks>
/// <para>
/// A suite of leak tests that all pass proves nothing unless the assertions it uses are known to fail when a leak is
/// present. The tests in this class supply that proof: they introduce a retention path deliberately and require the
/// assertions to detect it, to name the retaining field, and to distinguish a strong reference from a reference held
/// through a <see cref="ConditionalWeakTable{TKey,TValue}"/>.
/// </para>
/// <para>
/// The objects retained on purpose here are real <see cref="Compilation"/> instances, so that the size and shape of
/// the graph that the search has to traverse are representative of the graph in the tests that matter.
/// </para>
/// </remarks>
public sealed class MemoryLeakAssertSelfTests : DesignTimeTestBase
{
    public MemoryLeakAssertSelfTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// A component that retains every compilation it is given, standing in for a defective cache.
    /// </summary>
    private sealed class LeakingCache
    {
        /// <summary>
        /// Gets the list that retains the compilations. Its name appears in the retention path that the assertion
        /// reports, which is what the tests verify.
        /// </summary>
        public List<Compilation> RetainedCompilations { get; } = new();
    }

    /// <summary>
    /// A component that stores compilations in a <see cref="ConditionalWeakTable{TKey,TValue}"/> keyed by the
    /// compilation itself, which does not retain them.
    /// </summary>
    private sealed class ConditionalCache
    {
        public ConditionalWeakTable<Compilation, object> Entries { get; } = new();
    }

    /// <summary>
    /// Creates a compilation, adds it to a cache that retains it, and returns only a weak reference to it.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static WeakReference CreateAndRetain( TestContext testContext, LeakingCache cache, string assemblyName )
    {
        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["Code.cs"] = "public class C { }" },
            assemblyName: assemblyName );

        cache.RetainedCompilations.Add( compilation );

        return new WeakReference( compilation );
    }

    /// <summary>
    /// Creates a compilation and returns only a weak reference to it, retaining nothing.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static WeakReference CreateWithoutRetaining( TestContext testContext, string assemblyName )
    {
        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["Code.cs"] = "public class C { }" },
            assemblyName: assemblyName );

        return new WeakReference( compilation );
    }

    /// <summary>
    /// Creates a compilation, registers it in a conditional weak table keyed by itself, and returns only a weak
    /// reference to it.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static WeakReference CreateAndRegisterConditionally( TestContext testContext, ConditionalCache cache, string assemblyName )
    {
        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["Code.cs"] = "public class C { }" },
            assemblyName: assemblyName );

        cache.Entries.Add( compilation, new object() );

        return new WeakReference( compilation );
    }

    /// <summary>
    /// Verifies that a compilation which nothing retains is reported as collected.
    /// </summary>
    /// <remarks>
    /// This is the negative control. If it failed, every other test in the suite would fail for a reason unrelated to
    /// the code under test, such as a local variable of the test harness keeping the compilation alive.
    /// </remarks>
    [Fact]
    public void UnretainedCompilationIsReportedAsCollected()
    {
        using var testContext = this.CreateTestContext();

        var weakReference = CreateWithoutRetaining( testContext, nameof(this.UnretainedCompilationIsReportedAsCollected) );

        MemoryLeakAssert.Collected( weakReference, "An unretained compilation", ("testContext", testContext) );
    }

    /// <summary>
    /// Verifies that a compilation retained by a strong reference is detected, and that the failure message names the
    /// field that retains it.
    /// </summary>
    /// <remarks>
    /// This is the positive control, and it is the test that gives the rest of the suite its value. Without it, a
    /// suite in which every test passes would be indistinguishable from a suite whose assertions never fail.
    /// </remarks>
    [Fact]
    public void RetainedCompilationIsDetectedAndTheRetainingFieldIsNamed()
    {
        using var testContext = this.CreateTestContext();

        var cache = new LeakingCache();
        var weakReference = CreateAndRetain( testContext, cache, nameof(this.RetainedCompilationIsDetectedAndTheRetainingFieldIsNamed) );

        var exception = Assert.Throws<FailException>(
            () => MemoryLeakAssert.Collected( weakReference, "A deliberately retained compilation", ("leakingCache", cache) ) );

        this.TestOutput.WriteLine( exception.Message );

        Assert.Contains( "still reachable", exception.Message, StringComparison.Ordinal );
        Assert.Contains( "leakingCache", exception.Message, StringComparison.Ordinal );
        Assert.Contains( nameof(LeakingCache.RetainedCompilations), exception.Message, StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that the growth assertion detects the case in which every version is retained.
    /// </summary>
    [Fact]
    public void RetainedCompilationsAreDetectedByTheGrowthAssertion()
    {
        using var testContext = this.CreateTestContext();

        var cache = new LeakingCache();
        var weakReferences = new WeakReference[5];

        for ( var i = 0; i < weakReferences.Length; i++ )
        {
            weakReferences[i] = CreateAndRetain(
                testContext,
                cache,
                $"{nameof(this.RetainedCompilationsAreDetectedByTheGrowthAssertion)}{i}" );
        }

        var exception = Assert.Throws<FailException>(
            () => MemoryLeakAssert.AtMostAlive( weakReferences, 1, "deliberately retained compilations", ("leakingCache", cache) ) );

        this.TestOutput.WriteLine( exception.Message );

        Assert.Contains( "5 of 5", exception.Message, StringComparison.Ordinal );
        Assert.Contains( nameof(LeakingCache.RetainedCompilations), exception.Message, StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that a compilation registered as the key of a conditional weak table is not considered retained.
    /// </summary>
    /// <remarks>
    /// The design-time code relies on this property throughout, therefore a test suite that reported such an entry as
    /// a leak would produce a stream of false failures. This test pins the semantics that the other tests assume.
    /// </remarks>
    [Fact]
    public void ConditionallyRegisteredCompilationIsReportedAsCollected()
    {
        using var testContext = this.CreateTestContext();

        var cache = new ConditionalCache();

        var weakReference = CreateAndRegisterConditionally(
            testContext,
            cache,
            nameof(this.ConditionallyRegisteredCompilationIsReportedAsCollected) );

        MemoryLeakAssert.Collected( weakReference, "A compilation used as a conditional weak table key", ("conditionalCache", cache) );
    }
}
