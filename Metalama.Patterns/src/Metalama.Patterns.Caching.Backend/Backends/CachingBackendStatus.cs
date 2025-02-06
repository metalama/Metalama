// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// Statuses of a <see cref="CachingBackend"/>.
/// </summary>
public enum CachingBackendStatus
{
    /// <summary>
    /// Default. Just created.
    /// </summary>
    Default,

    /// <summary>
    /// Initializing.
    /// </summary>
    Initializing,

    /// <summary>
    /// Initialized.
    /// </summary>
    Initialized,

    Failed,

    /// <summary>
    /// Being currently disposed.
    /// </summary>
    Disposing,

    /// <summary>
    /// Already disposed.
    /// </summary>
    Disposed,

    /// <summary>
    /// A previous call of Dispose failed.
    /// </summary>
    DisposeFailed
}