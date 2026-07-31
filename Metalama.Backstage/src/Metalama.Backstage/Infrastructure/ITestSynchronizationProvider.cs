// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System.Threading;

namespace Metalama.Backstage.Infrastructure;

/// <summary>
/// Provides synchronization points, so that a test can drive concurrent code into a specific interleaving instead of
/// hoping for it. This service is never registered in production, so a sync point is a null check there.
/// </summary>
/// <remarks>
/// This mirrors the service of the same name in <c>Metalama.Framework.DesignTime.Rpc</c>, which cannot be referenced
/// from here. Add a sync point only where a race is otherwise not reproducible: each one is production code that exists
/// for a test, so it must be justified by a defect that escaped review. See #1764.
/// </remarks>
internal interface ITestSynchronizationProvider : IBackstageService
{
    /// <summary>
    /// Called by the code under test. Signals that the sync point was reached and blocks until the test releases it.
    /// Does nothing when the test has not enabled this particular sync point.
    /// </summary>
    /// <param name="syncPointName">
    /// A unique name identifying this sync point, in the form <c>{ClassName}.{Member}:{Location}</c>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    void SyncPoint( string syncPointName, CancellationToken cancellationToken = default );
}
