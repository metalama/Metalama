// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;

namespace Metalama.Mcp;

internal static class EmbeddedResources
{
    public static string Read( string name )
    {
        using var stream = typeof(EmbeddedResources).Assembly.GetManifestResourceStream( "Metalama.Mcp." + name )
                           ?? throw new InvalidOperationException( $"The embedded resource '{name}' does not exist." );

        using var reader = new StreamReader( stream );

        return reader.ReadToEnd();
    }
}
