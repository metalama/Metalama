// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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