// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Utilities;

/// <summary>
/// Extension methods for the <see cref="IPromise{T}"/> interface.
/// </summary>
[CompileTime]
[PublicAPI]
public static class PromiseExtensions
{
    /// <summary>
    /// Attempts to get the value of a promise if it is resolved.
    /// </summary>
    /// <typeparam name="T">The type of value contained in the promise.</typeparam>
    /// <param name="promise">The promise to get the value from.</param>
    /// <param name="value">When this method returns, contains the value if the promise is resolved; otherwise, the default value for type <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if the promise is resolved and the value was retrieved; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetValue<T>( this IPromise<T> promise, [MaybeNullWhen( false )] out T value )
    {
        if ( promise.IsResolved )
        {
            value = promise.Value;

            return true;
        }
        else
        {
            value = default;

            return false;
        }
    }
}