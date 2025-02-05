// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if NET5_0_OR_GREATER
using System;
#endif

namespace Metalama.Backstage;

internal static class StringExtensions
{
    public static bool ContainsOrdinal( this string? s, string substring )
#if NET5_0_OR_GREATER
        => s?.Contains( substring, StringComparison.Ordinal ) ?? false;
#else
        => s?.Contains( substring ) ?? false;
#endif
}