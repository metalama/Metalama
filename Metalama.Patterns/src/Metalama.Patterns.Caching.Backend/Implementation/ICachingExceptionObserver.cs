// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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