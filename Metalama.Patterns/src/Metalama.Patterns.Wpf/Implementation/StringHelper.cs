// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Wpf.Implementation;

[CompileTime]
internal static class StringHelper
{
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static bool TrimStart( ref string s, string trim, StringComparison stringComparison )
    {
        if ( s.StartsWith( trim, stringComparison ) )
        {
            s = s.Substring( trim.Length );

            return true;
        }

        return false;
    }

    public static bool TrimEnd( ref string s, string trim, StringComparison stringComparison )
    {
        if ( s.EndsWith( trim, stringComparison ) )
        {
            s = s.Substring( 0, s.Length - trim.Length );

            return true;
        }

        return false;
    }
}