// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Resilience;

/// <summary>
/// Determines how exceptions should be handled in <see cref="CachingBackend"/>.
/// </summary>
[PublicAPI]
public interface IExceptionHandlingPolicy
{
    /// <summary>
    /// Method invoked when an exception is thrown by the <see cref="CachingBackend"/> implementation.
    /// It should return a <see cref="RecoveryAction"/>.
    /// </summary>
    /// <param name="exceptionInfo">Information about the exception.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the current operation.</param>
    /// <returns>A <see cref="RecoveryAction"/>.</returns>
    ValueTask<RecoveryAction> OnExceptionAsync( ExceptionInfo exceptionInfo, CancellationToken cancellationToken );
}