// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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