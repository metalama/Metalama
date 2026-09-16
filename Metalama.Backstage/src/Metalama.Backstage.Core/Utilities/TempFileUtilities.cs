// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;

namespace Metalama.Backstage.Utilities;

/// <summary>
/// Creates temporary files in a given directory.
/// </summary>
internal static class TempFileUtilities
{
    /// <summary>
    /// Creates an empty file with a unique name in the given directory and returns its full path.
    /// </summary>
    /// <param name="directory">The directory, which is created when it does not exist.</param>
    public static string GetTempFileName( string directory )
    {
        Directory.CreateDirectory( directory );

        // https://stackoverflow.com/a/10152460/4100001
        var attempt = 0;

        while ( true )
        {
            var path = Path.Combine( directory, $"{Guid.NewGuid()}.tmp" );

            try
            {
                using ( var newFile = new FileStream( path, FileMode.Create ) )
                {
                    newFile.Close();
                }
            }
            catch ( IOException ) when ( ++attempt < 10 ) { continue; }

            return path;
        }
    }
}
