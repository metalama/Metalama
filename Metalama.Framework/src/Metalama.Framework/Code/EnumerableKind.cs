// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable InconsistentNaming

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code;

/// <summary>
/// Kinds of iterators.
/// </summary>
/// <remarks>
/// To get the <see cref="EnumerableKind"/> of a method, use the <see cref="MethodExtensions.GetIteratorInfo"/> extension method,
/// which returns an <see cref="IteratorInfo"/> struct. The <see cref="IteratorInfo.EnumerableKind"/> property contains the enumerable kind.
/// </remarks>
/// <seealso cref="IMethod"/>
/// <seealso cref="IteratorInfo"/>
/// <seealso cref="MethodExtensions.GetIteratorInfo"/>
/// <seealso cref="IFieldOrPropertyOrIndexer.GetMethod"/>
[CompileTime]
public enum EnumerableKind
{
    /// <summary>
    /// None. The method does not returns an enumerable or enumerator.
    /// </summary>
    None,

    /// <summary>
    /// A method returning a generic <see cref="System.Collections.Generic.IEnumerable{T}" />.
    /// </summary>
    IEnumerable,

    /// <summary>
    /// A method returning a generic <see cref="System.Collections.Generic.IEnumerator{T}" />.
    /// </summary>
    IEnumerator,

    /// <summary>
    /// A method returning a non-generic <see cref="System.Collections.IEnumerable" />.
    /// </summary>
    UntypedIEnumerable,

    /// <summary>
    /// A method returning a non-generic <see cref="System.Collections.IEnumerator" />.
    /// </summary>
    UntypedIEnumerator,

    /// <summary>
    /// A method returning <c>System.Collections.Generic.IAsyncEnumerable</c>.
    /// </summary>
    IAsyncEnumerable,

    /// <summary>
    /// A method returning <c>System.Collections.Generic.IAsyncEnumerator</c>.
    /// </summary>
    IAsyncEnumerator
}