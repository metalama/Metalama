// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Text;

namespace Flashtrace.Utilities;

[PublicAPI]
public static class CharSpanExtensions
{
    public static void PortableAppend( this StringBuilder stringBuilder, ReadOnlySpan<char> span )
    {
#if NET6_0_OR_GREATER
        stringBuilder.Append( span );
#else
        unsafe
        {
            fixed ( char* s = span )
            {
                stringBuilder.Append( s, span.Length );
            }
        }
#endif
    }
}