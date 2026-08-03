// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Utilities;
using Metalama.Testing.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Waits for the background tasks of a <see cref="TaskBag"/> before a test asserts on what is still reachable.
/// </summary>
/// <remarks>
/// <para>
/// A task of a bag removes its own entry when it runs, and it runs on the thread pool. An assertion made before the
/// task has run therefore observes a bag that is merely still busy, and its result depends on how loaded the thread
/// pool happens to be. That dependency is invisible when a test runs on its own and appears when the whole suite runs
/// in parallel, which is the worst way for it to appear.
/// </para>
/// <para>
/// A bag that strands a task leaves it in the canceled state, and <see cref="TaskBag.WaitAllAsync"/> surfaces that as
/// an exception. That exception is swallowed here, because it is the very defect that the assertions of the caller
/// diagnose, and their failure message names the field that retains the object. The cancellation of the test context
/// is not swallowed, so a bag that never completes fails the test rather than hanging it.
/// </para>
/// </remarks>
internal static class PendingTasksHelper
{
    /// <summary>
    /// Waits until every task of <paramref name="taskBag"/> has run.
    /// </summary>
    public static async Task WaitForPendingTasksAsync( TaskBag taskBag, TestContext testContext )
    {
        try
        {
            await taskBag.WaitAllAsync( testContext.CancellationToken );
        }
        catch ( OperationCanceledException ) when ( !testContext.CancellationToken.IsCancellationRequested )
        {
            // See the remarks on the class.
        }
    }
}
