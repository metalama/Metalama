// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Exposes a method <see cref="OnException"/> called when a <see cref="CachingBackend"/> encounters a recoverable error.
/// The default behavior is to log the error and continue the execution. An application can implement an observer
/// and register it to the <see cref="IServiceProvider"/>.
/// </summary>
public interface ICachingExceptionObserver
{
    /// <summary>
    /// Method called when a <see cref="CachingBackend"/> encounters a recoverable error.
    /// </summary>
    /// <param name="exceptionInfo">An <see cref="CachingExceptionInfo"/>.</param>
    void OnException( CachingExceptionInfo exceptionInfo );
}