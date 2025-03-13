// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Utilities;

internal static class ArrayHelper
{
    public static T[] Prepend<T>( this T[] array, T item )
    {
        var all = new T[array.Length + 1];
        all[0] = item;
        Array.Copy( array, 0, all, 1, array.Length );

        return all;
    }
}