// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Formatters;

internal static class StringExtensions
{
    /// <summary>
    /// Returns the zero-based index of the first occurrence of the specified character using ordinal comparison.
    /// </summary>
    public static int IndexOfOrdinal( this string str, char value )
    {
#if NET5_0_OR_GREATER
        return str.IndexOf( value, StringComparison.Ordinal );
#else
        return str.IndexOf( value );
#endif
    }
}