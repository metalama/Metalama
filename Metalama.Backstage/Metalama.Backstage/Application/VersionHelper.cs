// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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