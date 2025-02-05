// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.Application;

internal static class VersionHelper
{
#pragma warning disable CA1307
    public static bool IsPrereleaseVersion( string version ) => version.Contains( "-" ) && !version.EndsWith( "-rc", StringComparison.Ordinal );
#pragma warning restore CA1307

    public static bool IsDevelopmentVersion( string version )
    {
        var versionParts = version.Split( '-' );

        return versionParts.Length > 1 && versionParts[1] is "dev" or "local";
    }
}