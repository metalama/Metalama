// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.UnitTestHelpers.MemoryLeaks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
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
    private static WeakReference CreateAndRetain( MetalamaTestContext testContext, LeakingCache cache, string assemblyName )
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
    private static WeakReference CreateWithoutRetaining( MetalamaTestContext testContext, string assemblyName )
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
    private static WeakReference CreateAndRegisterConditionally( MetalamaTestContext testContext, ConditionalCache cache, string assemblyName )
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

    /// <summary>
    /// Verifies that <see cref="MemoryLeakAssert.Collected"/> fails, and that <see cref="MemoryLeakAssert.CollectedAsync"/>
    /// succeeds, when the test runs above a stack frame that references the object.
    /// </summary>
    /// <remarks>
    /// A dedicated thread completes a task while a local variable of its frame references an object. xunit v3 runs the
    /// test without a synchronization context, so the continuation of the test runs synchronously on that thread, inside
    /// the call that completes the task. This reproduces deterministically the situation that
    /// <see cref="MemoryLeakAssert.CollectedAsync"/> handles, in which the frame belongs to the code under test.
    /// </remarks>
    [Fact]
    public async Task CollectedAsync_LeavesTheFrameOfTheThreadThatResumedTheTest()
    {
        Assert.SkipUnless(
            SynchronizationContext.Current == null && TaskScheduler.Current == TaskScheduler.Default,
            "The continuation of an await runs synchronously on the completing thread only without a synchronization context." );

        var resumption = new TaskCompletionSource<WeakReference>();

        // The registration cancels the wait without changing how its continuation runs.
        using var cancellationRegistration = TestContext.Current.CancellationToken.Register( () => resumption.TrySetCanceled() );

        var thread = new Thread( () => CompleteWhileReferencingAnObject( resumption ) );

        // The thread is started only once the continuation of the test is registered, so that the task cannot complete
        // before the await, which would then continue synchronously on the thread of the test.
        var weakReference = await new StartThreadAfterRegistration( resumption.Task, thread );

        Assert.Equal( thread.ManagedThreadId, Environment.CurrentManagedThreadId );

        Assert.Throws<FailException>( () => MemoryLeakAssert.Collected( weakReference, "An object referenced by a frame below the test" ) );

        await MemoryLeakAssert.CollectedAsync( weakReference, "An object referenced by a frame below the test" );

        Assert.True( thread.Join( TimeSpan.FromMinutes( 1 ) ) );
    }

    /// <summary>
    /// An awaitable that registers the continuation of the caller on a task, and then starts the thread that completes
    /// the task.
    /// </summary>
    private sealed class StartThreadAfterRegistration : INotifyCompletion
    {
        /// <summary>
        /// The task whose completion resumes the caller.
        /// </summary>
        private readonly Task<WeakReference> _task;
        /// <summary>
        /// The thread that completes the task.
        /// </summary>
        private readonly Thread _thread;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartThreadAfterRegistration"/> class.
        /// </summary>
        public StartThreadAfterRegistration( Task<WeakReference> task, Thread thread )
        {
            this._task = task;
            this._thread = thread;
        }

        /// <summary>
        /// Gets the awaiter, which is this object.
        /// </summary>
        public StartThreadAfterRegistration GetAwaiter() => this;

        /// <summary>
        /// Gets a value indicating whether the task is complete, which is always <c>false</c>, so that the caller registers a continuation.
        /// </summary>
        public bool IsCompleted => false;

        /// <inheritdoc />
        public void OnCompleted( Action continuation )
        {
            this._task.GetAwaiter().OnCompleted( continuation );
            this._thread.Start();
        }

        /// <summary>
        /// Gets the result of the task.
        /// </summary>
        // The compiler calls GetResult only after the continuation has run, when the task is complete, so it does not block.
#pragma warning disable VSTHRD002
        public WeakReference GetResult() => this._task.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    /// <summary>
    /// Completes a task while a local variable of the current frame references the object whose weak reference is the
    /// result of the task.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static void CompleteWhileReferencingAnObject( TaskCompletionSource<WeakReference> resumption )
    {
        var referencedObject = new object();

        resumption.SetResult( new WeakReference( referencedObject ) );

        GC.KeepAlive( referencedObject );
    }
}
