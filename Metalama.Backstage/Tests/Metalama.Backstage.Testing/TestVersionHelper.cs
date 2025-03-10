// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Backstage.Testing;

internal static class TestVersionHelper
{
    public static Version? GetAssemblyVersionFromPackageVersion( string? packageVersion )
    {
        if ( packageVersion == null )
        {
            return null;
        }

#pragma warning disable CA1307
        var dashPosition = packageVersion.IndexOf( '-' );
#pragma warning restore CA1307

        if ( dashPosition < 0 )
        {
            return new Version( packageVersion );
        }
        else
        {
            return new Version( packageVersion.Substring( 0, dashPosition ) );
        }
    }
}