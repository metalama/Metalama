// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.Collections.Concurrent;

namespace Flashtrace.Formatters;

/// <summary>
/// A thread-safe pool of <see cref="UnsafeStringBuilder"/>.
/// </summary>
[PublicAPI]
public sealed class UnsafeStringBuilderPool : IDisposable
{
    private readonly ConcurrentBag<UnsafeStringBuilder> _instances = new();

    /// <summary>
    /// Gets the maximum number of characters in instances of the <see cref="UnsafeStringBuilder"/> class managed by the current pool.
    /// </summary>
    public int StringBuilderCapacity { get; }

    private readonly bool _throwOnOverflow;
    private readonly int _maxInstances;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsafeStringBuilderPool"/> class.
    /// </summary>
    /// <param name="stringBuilderCapacity">Maximal number of characters in the <see cref="UnsafeStringBuilder"/> in the pool.</param>
    /// <param name="throwOnOverflow"><c>true</c> if an <see cref="OverflowException"/> should be thrown when
    /// the buffer capacity is insufficient, <c>false</c> if the <c>Append</c> method should return <c>false</c> without exception.</param>
    /// <param name="maxInstances">Maximal number of instances in the pool. The default value is 32.</param>
    public UnsafeStringBuilderPool( int stringBuilderCapacity, bool throwOnOverflow, int maxInstances = 32 )
    {
        this.StringBuilderCapacity = stringBuilderCapacity;
        this._throwOnOverflow = throwOnOverflow;
        this._maxInstances = maxInstances;
    }

    /// <summary>
    /// Gets an instance from the pool.
    /// </summary>
    /// <returns>An <see cref="UnsafeStringBuilder"/>.</returns>
    public UnsafeStringBuilder GetInstance()
    {
        if ( !this._instances.TryTake( out var stringBuilder ) )
        {
            stringBuilder = new UnsafeStringBuilder( this.StringBuilderCapacity, this._throwOnOverflow );
        }

        return stringBuilder;
    }

    /// <summary>
    /// Returns an <see cref="UnsafeStringBuilder"/> to the pool.
    /// </summary>
    /// <param name="stringBuilder">An <see cref="UnsafeStringBuilder"/> that has compatible values of the <see cref="UnsafeStringBuilder.Capacity"/>
    /// and <see cref="UnsafeStringBuilder.ThrowOnOverflow"/>.</param>
    public void ReturnInstance( UnsafeStringBuilder stringBuilder )
    {
        if ( stringBuilder == null )
        {
            throw new ArgumentNullException( nameof(stringBuilder) );
        }

        if ( stringBuilder.IsDisposed )
        {
            throw new ObjectDisposedException( nameof(UnsafeStringBuilder) );
        }

        if ( stringBuilder.Capacity != this.StringBuilderCapacity || stringBuilder.ThrowOnOverflow != this._throwOnOverflow )
        {
            throw new ArgumentOutOfRangeException( nameof(stringBuilder) );
        }

        if ( this._instances.Count >= this._maxInstances )
        {
            stringBuilder.Dispose();
        }
        else
        {
            stringBuilder.Clear();
            this._instances.Add( stringBuilder );
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        while ( this._instances.TryTake( out var stringBuilder ) )
        {
            stringBuilder.Dispose();
        }
    }
}