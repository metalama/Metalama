// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code;

/// <summary>
/// Provides information about the iterator characteristics of a method, returned by the <see cref="MethodExtensions.GetIteratorInfo"/> extension method of <see cref="IMethod"/>.
/// </summary>
/// <remarks>
/// <para>Use this struct to inspect iterator method properties at compile time, for example in <c>BuildAspect</c>.</para>
/// <para>Note that <see cref="IsIteratorMethod"/> indicates whether the method uses <c>yield return</c> or <c>yield break</c>,
/// while <see cref="EnumerableKind"/> indicates the return type regardless of the implementation.</para>
/// </remarks>
/// <seealso cref="MethodExtensions.GetIteratorInfo"/>
/// <seealso cref="EnumerableKind"/>
/// <seealso cref="AsyncInfo"/>
/// <seealso href="@overriding-methods#async-iterator-default-template"/>
[CompileTime]
public readonly struct IteratorInfo
{
    private readonly IType? _returnType;

    /// <summary>
    /// Gets a value indicating whether the method is an iterator, i.e., whether it has a <c>yield return</c> or <c>yield break</c> statement.
    /// </summary>
    /// <value>
    /// <c>true</c> if the method uses <c>yield return</c> or <c>yield break</c>;
    /// <c>false</c> if the method returns an enumerable type but does not use <c>yield</c>;
    /// <c>null</c> if the method is not defined in the current project and the information is not available.
    /// </value>
    /// <remarks>
    /// This property only indicates whether the method uses yield-based iteration. To determine the kind of enumerable
    /// returned by the method regardless of implementation, use <see cref="EnumerableKind"/>.
    /// </remarks>
    public bool? IsIteratorMethod { get; }

    /// <summary>
    /// Gets the type of items being enumerated.
    /// </summary>
    /// <value>
    /// The item type. For example, for a method returning <c>IEnumerable&lt;int&gt;</c>, this property returns <c>int</c>.
    /// For non-generic enumerable types like <see cref="System.Collections.IEnumerable"/>, this property returns <see cref="object"/>.
    /// </value>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="EnumerableKind"/> is <see cref="Code.EnumerableKind.None"/>.</exception>
    public IType ItemType
    {
        get
        {
            if ( this._returnType == null )
            {
                throw new InvalidOperationException( $"Cannot get the {nameof(this.ItemType)} property because the return type is not available." );
            }

            if ( this._returnType is INamedType { TypeArguments.Count: > 0 } namedType )
            {
                return namedType.TypeArguments[0];
            }
            else
            {
                return TypeFactory.GetType( SpecialType.Object );
            }
        }
    }

    /// <summary>
    /// Gets the kind of enumerable or enumerator returned by the method, regardless of whether the method is a yield-based iterator (see <see cref="IsIteratorMethod"/>).
    /// </summary>
    /// <value>
    /// One of the <see cref="Code.EnumerableKind"/> values: <see cref="Code.EnumerableKind.None"/>, <see cref="Code.EnumerableKind.IEnumerable"/>,
    /// <see cref="Code.EnumerableKind.IEnumerator"/>, <see cref="Code.EnumerableKind.UntypedIEnumerable"/>, <see cref="Code.EnumerableKind.UntypedIEnumerator"/>,
    /// <see cref="Code.EnumerableKind.IAsyncEnumerable"/>, or <see cref="Code.EnumerableKind.IAsyncEnumerator"/>.
    /// </value>
    public EnumerableKind EnumerableKind { get; }

    internal IteratorInfo( bool? isIteratorMethod, EnumerableKind enumerableKind, IType? returnType )
    {
        this._returnType = returnType;
        this.EnumerableKind = enumerableKind;
        this.IsIteratorMethod = isIteratorMethod;
    }
}