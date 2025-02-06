// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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