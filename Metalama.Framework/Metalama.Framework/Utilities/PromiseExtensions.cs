// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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