// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Arguments of <see cref="ICachingExceptionObserver.OnException"/>.
/// </summary>
[PublicAPI]
public sealed class CachingExceptionInfo
{
    internal CachingExceptionInfo( Exception exception, bool affectsCacheConsistency )
    {
        this.Exception = exception;
        this.AffectsCacheConsistency = affectsCacheConsistency;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the exception should be rethrown. The default value is <c>false</c>.
    /// </summary>
    public bool Rethrow { get; set; }

    /// <summary>
    /// Gets the exception.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets a value indicating whether the consistency of the distributed cache is affected by the failure.
    /// </summary>
    public bool AffectsCacheConsistency { get; }
}