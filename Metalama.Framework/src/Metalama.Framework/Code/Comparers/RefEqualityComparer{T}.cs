// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Framework.Code.Comparers;

/// <summary>
/// A strongly-typed implementation of <see cref="IEqualityComparer{T}"/> for <see cref="IRef{T}"/> that compares
/// references based on <see cref="RefComparison"/> semantics.
/// </summary>
/// <typeparam name="T">The type of compilation element referenced, such as <see cref="IMethod"/>,
/// <see cref="IProperty"/>, <see cref="INamedType"/>, or any other <see cref="ICompilationElement"/>.</typeparam>
/// <remarks>
/// <para>
/// This is the strongly-typed variant of <see cref="RefEqualityComparer"/>. It allows using <see cref="IRef{T}"/>
/// instances as keys in dictionaries or elements in hash sets while maintaining type safety.
/// </para>
/// <para>
/// Use the appropriate static property to obtain a comparer with the desired comparison strategy:
/// </para>
/// <list type="bullet">
/// <item>
/// <description><see cref="Default"/>: Fast comparison within the same compilation, ignores nullability.</description>
/// </item>
/// <item>
/// <description><see cref="IncludeNullability"/>: Same compilation, considers nullable annotations.</description>
/// </item>
/// <item>
/// <description><see cref="Structural"/>: Cross-compilation comparison, ignores nullability.</description>
/// </item>
/// <item>
/// <description><see cref="StructuralIncludeNullability"/>: Cross-compilation, considers nullable annotations.</description>
/// </item>
/// </list>
/// </remarks>
/// <seealso cref="RefEqualityComparer"/>
/// <seealso cref="RefComparison"/>
/// <seealso cref="IRef{T}"/>
public sealed class RefEqualityComparer<T> : IEqualityComparer<IRef<T>?>, IRefEqualityComparer
    where T : class, ICompilationElement
{
    private readonly RefComparison _comparison;

    /// <summary>
    /// Gets the default equality comparer that uses <see cref="RefComparison.Default"/> semantics.
    /// This is the fastest comparer and is suitable for comparing references within the same compilation.
    /// </summary>
    public static RefEqualityComparer<T> Default { get; } = new( RefComparison.Default );

    /// <summary>
    /// Gets an equality comparer that uses <see cref="RefComparison.IncludeNullability"/> semantics.
    /// Use when nullable reference type annotations should affect equality.
    /// </summary>
    public static RefEqualityComparer<T> IncludeNullability { get; } = new( RefComparison.IncludeNullability );

    /// <summary>
    /// Gets an equality comparer that uses <see cref="RefComparison.Structural"/> semantics.
    /// Use when comparing references across different compilation versions.
    /// </summary>
    public static RefEqualityComparer<T> Structural { get; } = new( RefComparison.Structural );

    /// <summary>
    /// Gets an equality comparer that uses <see cref="RefComparison.StructuralIncludeNullability"/> semantics.
    /// Use when comparing references across different compilations and nullable annotations matter.
    /// </summary>
    public static RefEqualityComparer<T> StructuralIncludeNullability { get; } =
        new( RefComparison.StructuralIncludeNullability );

    private RefEqualityComparer( RefComparison comparison )
    {
        this._comparison = comparison;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public int GetHashCode( IRef<T> obj ) => obj.GetHashCode( this._comparison );
}