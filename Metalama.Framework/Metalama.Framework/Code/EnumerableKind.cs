// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// ReSharper disable InconsistentNaming

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code;

/// <summary>
/// Kinds of iterators.
/// </summary>
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