// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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