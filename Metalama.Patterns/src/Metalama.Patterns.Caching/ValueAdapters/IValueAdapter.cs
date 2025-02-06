// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.Collections;

namespace Metalama.Patterns.Caching.ValueAdapters;

/// <summary>
/// A strongly-typed version of <see cref="IValueAdapter"/>.
/// </summary>
/// <typeparam name="T">Type of the exposed value, i.e. typically return type of the cached method.</typeparam>
[PublicAPI]
public interface IValueAdapter<T> : IValueAdapter
{
    /// <summary>
    /// Gets the value that should be stored in the cache.
    /// </summary>
    /// <param name="value">The apparent value (typically the return value of the cached method).</param>
    /// <returns>A cacheable object.</returns>
    object? GetStoredValue( T? value );

    /// <summary>
    /// Asynchronously gets the value that should be stored in the cache.
    /// </summary>
    /// <param name="value">The apparent value (typically the return value of the cached method).</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> returning a cacheable object.</returns>
    Task<object?> GetStoredValueAsync( T? value, CancellationToken cancellationToken );

    /// <summary>
    /// Gets the value that should be exposed to the consuming application, i.e. typically the return value of the cached method.
    /// </summary>
    /// <param name="storedValue">The value that was stored in the cache.</param>
    /// <returns>The value that should be exposed to the consuming application, i.e. typically the return value of the cached method.</returns>
    new T? GetExposedValue( object? storedValue );
}

/// <summary>
/// Wraps uncacheable classes or interfaces into cacheable objects, for instance an <see cref="IEnumerable"/> may be wrapped into an array.
/// </summary>
[PublicAPI]
public interface IValueAdapter
{
    /// <summary>
    /// Gets a value indicating whether the <see cref="GetStoredValueAsync"/> method is supported.
    /// </summary>
    bool IsAsyncSupported { get; }

    /// <summary>
    /// Gets the value that should be stored in the cache.
    /// </summary>
    /// <param name="value">The apparent value (typically the return value of the cached method).</param>
    /// <returns>A cacheable object.</returns>
    object? GetStoredValue( object? value );

    /// <summary>
    /// Asynchronously gets the value that should be stored in the cache.
    /// </summary>
    /// <param name="value">The apparent value (typically the return value of the cached method).</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> returning a cacheable object.</returns>
    Task<object?> GetStoredValueAsync( object? value, CancellationToken cancellationToken );

    /// <summary>
    /// Gets the value that should be exposed to the consuming application, i.e. typically the return value of the cached method.
    /// </summary>
    /// <param name="storedValue">The value that was stored in the cache.</param>
    /// <returns>The value that should be exposed to the consuming application, i.e. typically the return value of the cached method.</returns>
    object? GetExposedValue( object? storedValue );
}