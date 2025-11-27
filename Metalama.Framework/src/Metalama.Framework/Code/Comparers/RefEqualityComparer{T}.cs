// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Framework.Code.Comparers;

/// <summary>
/// An implementation of <see cref="IEqualityComparer{T}"/> for <see cref="IRef{T}"/> that compares references based on <see cref="RefComparison"/> semantics.
/// </summary>
/// <typeparam name="T">The type of compilation element referenced.</typeparam>
public sealed class RefEqualityComparer<T> : IEqualityComparer<IRef<T>?>, IRefEqualityComparer
    where T : class, ICompilationElement
{
    private readonly RefComparison _comparison;

    /// <summary>
    /// Gets the default equality comparer that uses <see cref="RefComparison.Default"/> semantics.
    /// </summary>
    public static RefEqualityComparer<T> Default { get; } = new( RefComparison.Default );

    /// <summary>
    /// Gets an equality comparer that uses <see cref="RefComparison.IncludeNullability"/> semantics.
    /// </summary>
    public static RefEqualityComparer<T> IncludeNullability { get; } = new( RefComparison.IncludeNullability );

    /// <summary>
    /// Gets an equality comparer that uses <see cref="RefComparison.Structural"/> semantics.
    /// </summary>
    public static RefEqualityComparer<T> Structural { get; } = new( RefComparison.Structural );

    /// <summary>
    /// Gets an equality comparer that uses <see cref="RefComparison.StructuralIncludeNullability"/> semantics.
    /// </summary>
    public static RefEqualityComparer<T> StructuralIncludeNullability { get; } =
        new( RefComparison.StructuralIncludeNullability );

    private RefEqualityComparer( RefComparison comparison )
    {
        this._comparison = comparison;
    }

    public bool Equals( IRef<T>? x, IRef<T>? y )
    {
        if ( ReferenceEquals( x, y ) )
        {
            return true;
        }

        if ( x == null || y == null )
        {
            return false;
        }

        return x.Equals( y, this._comparison );
    }

    public int GetHashCode( IRef<T> obj ) => obj.GetHashCode( this._comparison );
}