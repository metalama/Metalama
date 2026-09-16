// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using System;
using System.IO;

namespace Metalama.Backstage.Tools;

/// <summary>
/// The implementation of <see cref="IBackstageToolsLocator"/> of a development environment, which finds the worker in
/// the build output of the current repository instead of extracting it.
/// </summary>
internal sealed class DevBackstageToolsLocator : IBackstageToolsLocator
{
#if DEBUG
    private const string _buildConfiguration = "Debug";
#else
    private const string _buildConfiguration = "Release";
#endif

    private static readonly string _rootDirectory = FindRootDirectory();

    private readonly ProductProfile _productProfile;

    public DevBackstageToolsLocator( IServiceProvider serviceProvider )
    {
        this._productProfile = serviceProvider.GetRequiredBackstageService<ProductProfile>();
    }

    /// <summary>
    /// Finds the root of the repository from which the current program runs. The <c>.git</c> entry is a directory in a
    /// primary checkout and a file in a git worktree.
    /// </summary>
    private static string FindRootDirectory()
    {
        for ( var directory = Path.GetDirectoryName( Environment.GetCommandLineArgs()[0] ); directory != null; directory = Path.GetDirectoryName( directory ) )
        {
            var gitEntry = Path.Combine( directory, ".git" );

            if ( Directory.Exists( gitEntry ) || File.Exists( gitEntry ) )
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
            // The published output of the worker project, which contains the static web assets.
            return Path.Combine(
                _rootDirectory,
                "Metalama.Backstage",
                "src",
                tool.GetAssemblyName( this._productProfile ),
                "bin",
                _buildConfiguration,
                "net10.0",
                "Packed" );
        }
        else
        {
            throw new ArgumentOutOfRangeException( nameof(tool), $"The tool '{tool}' is not available in the development environment." );
        }
    }
}
