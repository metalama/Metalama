// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.ValueAdapters;

/// <summary>
/// An abstract implementation of <see cref="IValueAdapter{T}"/>.
/// </summary>
/// <typeparam name="T">Type of the exposed value, i.e. typically return type of the cached method.</typeparam>
[PublicAPI]
public abstract class ValueAdapter<T> : IValueAdapter<T>
{
    /// <inheritdoc />
    public virtual bool IsAsyncSupported => false;

    /// <inheritdoc />
    object? IValueAdapter.GetStoredValue( object? value ) => this.GetStoredValue( (T?) value );

    /// <inheritdoc />
    Task<object?> IValueAdapter.GetStoredValueAsync( object? value, CancellationToken cancellationToken )
        => this.GetStoredValueAsync( (T?) value, cancellationToken );

    /// <inheritdoc />
    public abstract T? GetExposedValue( object? storedValue );

    /// <inheritdoc />
    public abstract object? GetStoredValue( T? value );

    /// <inheritdoc />
    public virtual Task<object?> GetStoredValueAsync( T? value, CancellationToken cancellationToken ) => Task.FromResult( this.GetStoredValue( value ) );

    /// <inheritdoc />
    object? IValueAdapter.GetExposedValue( object? storedValue ) => this.GetExposedValue( storedValue );
}