// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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