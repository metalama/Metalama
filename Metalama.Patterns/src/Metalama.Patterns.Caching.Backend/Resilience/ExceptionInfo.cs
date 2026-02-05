// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace;
using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Resilience;

/// <summary>
/// Argument of the <see cref="IExceptionHandlingPolicy.OnExceptionAsync"/> method, describing the current exception.
/// </summary>
[PublicAPI]
public sealed class ExceptionInfo
{
    /// <summary>
    /// Gets the <see cref="System.Exception"/>.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets an object allowing to log the exception.
    /// </summary>
    public FlashtraceSource Logger { get; }

    /// <summary>
    /// Gets the kind of operation that caused the exception.
    /// </summary>
    public OperationKind OperationKind { get; }

    /// <summary>
    /// Gets the cache key that caused the exception.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// Gets the <see cref="Implementation.CacheItem"/> (only when <see cref="OperationKind"/> is <see cref="Resilience.OperationKind.SetItem"/>, otherwise <c>null</c>).
    /// </summary>
    public CacheItem? CacheItem { get; }

    internal ExceptionInfo( Exception exception, FlashtraceSource logger, OperationKind operationKind, string? key, CacheItem? cacheItem )
    {
        this.Exception = exception;
        this.Logger = logger;
        this.OperationKind = operationKind;
        this.Key = key;
        this.CacheItem = cacheItem;
    }
}