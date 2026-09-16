// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace LangVersionClamp.CSharp15;

public static class Class1
{
    /// <summary>
    /// Returns a value computed by a loop that a labeled jump leaves. The labeled break and the labeled continue
    /// are C# 15, so this method compiles only if the implied language version of the project survived the clamp of
    /// Metalama.Framework.targets.
    /// </summary>
    public static int Value()
    {
        var total = 0;

    outer:

        for ( var i = 0; i < 3; i++ )
        {
            for ( var j = 0; j < 3; j++ )
            {
                if ( j == 1 )
                {
                    continue outer;
                }

                total += i + j;

                if ( total > 4 )
                {
                    break outer;
                }
            }
        }

        return total;
    }
}
