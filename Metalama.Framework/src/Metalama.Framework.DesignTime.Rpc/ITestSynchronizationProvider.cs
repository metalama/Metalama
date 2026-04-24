// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Provides synchronization points for deterministic testing of concurrent code.
/// This service is optional and not registered in production. When not present,
/// code should skip synchronization points entirely.
/// </summary>
public interface ITestSynchronizationProvider
{
    /// <summary>
    /// Called at a synchronization point. Signals that the sync point was reached and waits for release.
    /// </summary>
    /// <param name="syncPointName">A unique name identifying this sync point, typically in format <c>{ClassName}.{Location}:{Context}</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SyncPointAsync( string syncPointName, CancellationToken cancellationToken );
}