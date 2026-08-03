// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Tests that <see cref="TaskBag"/> does not retain the tasks it was given once they can no longer run.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TaskBag"/> holds, for every pending task, both the <see cref="Task"/> and the
/// <see cref="Func{TResult}"/> that produced it, and removes the entry from a <c>finally</c> block that runs at the
/// end of the delegate. The source generator of the analysis process enqueues one such task per call to
/// <c>GenerateSources</c>, that is, once per keystroke, and the delegate it enqueues is a closure over the
/// <see cref="Compilation"/> that Roslyn has just produced. An entry that is never removed therefore retains a whole
/// version of the project.
/// </para>
/// <para>
/// The same source generator cancels the token of the previous call before enqueuing the next one, so that only the
/// most recent version is computed. A user who types faster than the pipeline runs produces a long sequence of tasks
/// that are cancelled before they start. This is the situation these tests reproduce, and it is the reason why the
/// growth is difficult to observe in a laboratory: a scenario that submits one version at a time, and waits for each
/// of them, never cancels anything.
/// </para>
/// </remarks>
public sealed class TaskBagMemoryLeakTests : DesignTimeTestBase
{
    public TaskBagMemoryLeakTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Creates a <see cref="TaskBag"/> that uses the services of a test context.
    /// </summary>
    private static TaskBag CreateTaskBag( TestContext testContext )
    {
        GlobalServiceProvider serviceProvider = testContext.ServiceProvider;

        return new TaskBag( serviceProvider.GetLoggerFactory().GetLogger( nameof(TaskBagMemoryLeakTests) ), serviceProvider );
    }

    /// <summary>
    /// Enqueues one task whose delegate captures a compilation, and returns only a weak reference to that
    /// compilation.
    /// </summary>
    /// <remarks>
    /// The delegate is deliberately shaped like the one that the source generator of the analysis process enqueues:
    /// it closes over the compilation that is being analysed.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static WeakReference EnqueueTaskCapturingACompilation(
        TestContext testContext,
        TaskBag taskBag,
        string assemblyName,
        CancellationToken cancellationToken )
    {
        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["Code.cs"] = "public class C { }" },
            assemblyName: assemblyName );

        taskBag.Enqueue( () => ObserveAsync( compilation ), cancellationToken );

        return new WeakReference( compilation );
    }

    /// <summary>
    /// The body of the enqueued task. It exists only so that the delegate has a reason to capture the compilation.
    /// </summary>
    private static Task ObserveAsync( Compilation compilation )
    {
        _ = compilation.AssemblyName;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that a task that completes normally is removed from the bag and does not retain what it captured.
    /// </summary>
    /// <remarks>
    /// This is the control for <see cref="CancelledBeforeStart_TaskIsRemovedFromTheBag"/>. It establishes that the
    /// removal mechanism works on the ordinary path, so that a failure of the other test is attributable to
    /// cancellation rather than to the test method itself.
    /// </remarks>
    [Fact]
    public async Task CompletedTask_IsRemovedFromTheBag()
    {
        using var testContext = this.CreateTestContext();
        var taskBag = CreateTaskBag( testContext );

        var compilation = EnqueueTaskCapturingACompilation(
            testContext,
            taskBag,
            nameof(this.CompletedTask_IsRemovedFromTheBag),
            CancellationToken.None );

        await taskBag.WaitAllAsync();

        Assert.True( taskBag.IsEmpty, "The bag still holds an entry for a task that has completed." );

        MemoryLeakAssert.Collected( compilation, "The compilation captured by a completed task", ("taskBag", taskBag) );
    }

    /// <summary>
    /// Verifies that a task whose cancellation token is already signalled when it is enqueued does not stay in the
    /// bag.
    /// </summary>
    /// <remarks>
    /// <see cref="Task.Run(Func{Task},CancellationToken)"/> does not invoke the delegate at all when the token is
    /// already signalled: the task goes directly to the canceled state. The <c>finally</c> block that removes the
    /// entry from the bag is part of that delegate, therefore it never runs, and the entry stays in the bag together
    /// with the closure that produced it.
    /// </remarks>
    [Fact]
    public void CancelledBeforeStart_TaskIsRemovedFromTheBag()
    {
        using var testContext = this.CreateTestContext();
        var taskBag = CreateTaskBag( testContext );

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var compilation = EnqueueTaskCapturingACompilation(
            testContext,
            taskBag,
            nameof(this.CancelledBeforeStart_TaskIsRemovedFromTheBag),
            cancellationTokenSource.Token );

        // The memory consequence is asserted first, because its failure message contains the chain of references that
        // retains the compilation, which is the information needed to act on the defect.
        MemoryLeakAssert.Collected( compilation, "The compilation captured by a task cancelled before it started", ("taskBag", taskBag) );

        Assert.True(
            taskBag.IsEmpty,
            "The bag holds an entry for a task that was cancelled before it started, and nothing will ever remove it." );
    }

    /// <summary>
    /// Verifies that a sequence of tasks cancelled before they start, which is what fast typing produces, does not
    /// accumulate in the bag.
    /// </summary>
    /// <remarks>
    /// The single-task case states the defect, and this one states its consequence: the number of retained versions
    /// grows with the number of keystrokes, which is the shape of the growth that has been reported.
    /// </remarks>
    [Fact]
    public void RepeatedCancellationBeforeStart_DoesNotAccumulate()
    {
        using var testContext = this.CreateTestContext();
        var taskBag = CreateTaskBag( testContext );

        const int taskCount = 20;
        var compilations = new WeakReference[taskCount];

        for ( var i = 0; i < taskCount; i++ )
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            compilations[i] = EnqueueTaskCapturingACompilation(
                testContext,
                taskBag,
                $"{nameof(this.RepeatedCancellationBeforeStart_DoesNotAccumulate)}{i}",
                cancellationTokenSource.Token );
        }

        MemoryLeakAssert.AtMostAlive(
            compilations,
            0,
            "compilations captured by tasks cancelled before they started",
            ("taskBag", taskBag) );

        Assert.True( taskBag.IsEmpty, $"The bag holds entries for {taskCount} tasks that were cancelled before they started." );
    }
}
