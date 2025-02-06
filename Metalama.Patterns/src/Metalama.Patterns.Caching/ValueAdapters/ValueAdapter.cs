// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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