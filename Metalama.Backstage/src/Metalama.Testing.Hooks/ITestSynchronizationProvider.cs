// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Testing.Hooks;

/// <summary>
/// Provides synchronization points, so that a test can drive concurrent code into a specific interleaving instead of
/// waiting for it to happen.
/// </summary>
/// <remarks>
/// <para>
/// This service is optional and is never registered in production. When it is absent, the code under test skips its
/// synchronization points entirely, so a synchronization point costs a null check.
/// </para>
/// <para>
/// The interface deliberately does not derive from any dependency injection marker interface, such as
/// <c>IBackstageService</c> or <c>IGlobalService</c>, because these are declared in the layers above this package.
/// Each layer therefore registers and resolves this service through its untyped methods.
/// </para>
/// <para>
/// Add a synchronization point only where a race is otherwise not reproducible: each one is production code that
/// exists for a test, so it must be justified by a defect that escaped review.
/// </para>
/// </remarks>
/// <seealso cref="TestSynchronizationProvider"/>
public interface ITestSynchronizationProvider
{
    /// <summary>
    /// Called by the code under test at an asynchronous synchronization point. Signals that the synchronization point
    /// was reached, then waits until the test releases it. Returns immediately for synchronization points that the
    /// test has not enabled.
    /// </summary>
    /// <param name="syncPointName">A unique name identifying this synchronization point, typically in the form <c>{ClassName}.{Member}:{Location}</c>.</param>
    /// <param name="cancellationToken">Cancellation token. It can be omitted at the many call sites that have no token at hand, because the test provider releases every synchronization point when it is disposed.</param>
    Task SyncPointAsync( string syncPointName, CancellationToken cancellationToken = default );

    /// <summary>
    /// Synchronous variant of <see cref="SyncPointAsync"/>, for synchronization points that must block while a lock is
    /// held, where awaiting is not possible. Signals that the synchronization point was reached, then blocks the
    /// current thread until the test releases it.
    /// </summary>
    /// <param name="syncPointName">A unique name identifying this synchronization point, typically in the form <c>{ClassName}.{Member}:{Location}</c>.</param>
    /// <param name="cancellationToken">Cancellation token. It can be omitted at the many call sites that have no token at hand, because the test provider releases every synchronization point when it is disposed.</param>
    void SyncPoint( string syncPointName, CancellationToken cancellationToken = default );
}
