// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// There is a copy of this code in Metalama.Compiler.Shared and partially in Metalama ResourceExtractor.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using System;
using System.IO;

namespace Metalama.Backstage.Utilities;

[PublicAPI]
public static class MetalamaPathUtilities
{
    private static readonly string? _overriddenTempPath;

    static MetalamaPathUtilities()
    {
        var overriddenTempPath = Environment.GetEnvironmentVariable( "METALAMA_TEMP" );
        _overriddenTempPath = string.IsNullOrEmpty( overriddenTempPath ) ? null : overriddenTempPath;
    }

    [Obsolete( "Use GetTempDirectory() or IStandardDirectories.TempDirectory. GetTempPath() is rooted in the world-writable /tmp on Unix (issue #1650)." )]
    public static string GetTempPath() => _overriddenTempPath ?? Path.GetTempPath();

    /// <summary>
    /// Gets the Metalama temporary directory. The actual logic lives in <see cref="IStandardDirectories.TempDirectory" />;
    /// this static accessor delegates to it for callers that do not have a service provider at hand. It therefore requires
    /// the backstage services to be initialized.
    /// </summary>
    public static string GetTempDirectory()
        => BackstageServiceFactory.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>().TempDirectory;

    public static string GetTempFileName()
    {
        var directory = GetTempDirectory();
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
