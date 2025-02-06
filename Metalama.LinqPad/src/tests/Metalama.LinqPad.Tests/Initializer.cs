// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.Build.Locator;

namespace Metalama.LinqPad.Tests;

internal static class Initializer
{
    public static void Initialize()
    {
        if ( MSBuildLocator.IsRegistered )
        {
            MSBuildLocator.RegisterDefaults();
        }
    }
}