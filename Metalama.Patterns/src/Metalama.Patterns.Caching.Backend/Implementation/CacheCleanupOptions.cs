// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Options for cache cleanup operations.
/// </summary>
[PublicAPI]
public sealed class CacheCleanupOptions
{
    /// <summary>
    /// Gets or sets the delay between the processing of two keys. The default value is 0.
    /// </summary>
    /// <remarks>With the Redis backend, when a cluster is used, all servers are processed concurrently and the delay applies while processing the requests
    /// of a single server.</remarks>
    public TimeSpan WaitDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets the delay between the moment an inconsistency is detected and moment a remediation is attempted. The default value is 10 seconds.
    /// </summary>
    /// <remarks>
    /// In distributed caching, there might be a slight delay between the changes are replicated among all nodes.
    /// These inconsistencies are harmless. This is why we test for a consistency a second time after some delay when we find some problem.
    /// </remarks>
    public TimeSpan RemediationDelay { get; set; } = TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// Gets or sets a value indicating whether the cleanup operation should report errors but not attempt to fix them.
    /// </summary>
    public bool Dry { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of keys being analyzed and cleaned up concurrently. The default value is 20.
    /// </summary>
    public int MaxConcurrency { get; set; } = 20;

    /// <summary>
    /// Get the default options.
    /// </summary>
    public static CacheCleanupOptions Default { get; } = new();
}