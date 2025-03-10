// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;

namespace Metalama.Backstage.Tools;

internal sealed class DevBackstageToolsLocator : IBackstageToolsLocator
{
#if DEBUG
    private const string _buildConfiguration = "Debug";
#else
    private const string _buildConfiguration = "Release";
#endif

    private static readonly string _rootDirectory = FindRootDirectory();

    private static string FindRootDirectory()
    {
        for ( var directory = Path.GetDirectoryName( Environment.GetCommandLineArgs()[0] ); directory != null; directory = Path.GetDirectoryName( directory ) )
        {
            if ( Directory.Exists( Path.Combine( directory, ".git" ) ) )
            {
                return directory;
            }
        }

        throw new FileNotFoundException( "Cannot find the repo directory." );
    }

    public bool ToolsMustBeExtracted => false;

    public string GetToolDirectory( BackstageTool tool )
    {
        if ( tool == BackstageTool.Worker )
        {
            return Path.Combine( _rootDirectory, tool.Name, "bin", _buildConfiguration, "net6.0", "Packed" );
        }
        else
        {
            throw new ArgumentOutOfRangeException();
        }
    }
}