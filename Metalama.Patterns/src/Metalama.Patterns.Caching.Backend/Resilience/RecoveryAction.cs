// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Resilience;

/// <summary>
/// Remediation actions when an exception occurs in a <see cref="CachingBackend"/>.
/// Returned by <see cref="IExceptionHandlingPolicy"/>.
/// </summary>
[PublicAPI]
public enum RecoveryAction
{
    /// <summary>
    /// Swallow.
    /// </summary>
    Default,

    /// <summary>
    /// Ignore the exception and continue.
    /// </summary>
    Swallow = Default,

    /// <summary>
    /// Rethrows the exception.
    /// </summary>
    Rethrow,

    /// <summary>
    /// Enqueue a task to remove the item in the background, and continue.
    /// </summary>
    RemoveItemInBackground,

    /// <summary>
    /// Enqueue a task to remove invalidate the dependency in the background, and continue.
    /// </summary>
    InvalidateDependencyInBackground
}