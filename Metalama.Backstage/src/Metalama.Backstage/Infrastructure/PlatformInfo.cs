// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.Infrastructure
{
    internal sealed class PlatformInfo : IPlatformInfo
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnvironmentVariableProvider _environmentVariableProvider;
        private readonly IFileSystem _fileSystem;
        private readonly IRuntimeInformation _runtimeInformation;
        private readonly Lazy<string> _dotNetExePath;

        public string DotNetExePath => this._dotNetExePath.Value;

        public PlatformInfo( IServiceProvider serviceProvider )
        {
            this._serviceProvider = serviceProvider;
            this._environmentVariableProvider = serviceProvider.GetRequiredBackstageService<IEnvironmentVariableProvider>();
            this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
            this._runtimeInformation = serviceProvider.GetRequiredBackstageService<IRuntimeInformation>();

            this._dotNetExePath = new Lazy<string>( this.GetDotNetPath );
        }

        private string GetDotNetPath()
        {
            var logger = this._serviceProvider.GetBackstageService<EarlyLoggerFactory>()?.GetLogger( "PlatformInfo" );

            var dotnetFileName = this._runtimeInformation.IsOSPlatform( OSPlatform.Windows )
                ? "dotnet.exe"
                : "dotnet";

            logger?.Trace?.Log( $"Looking for {dotnetFileName} path." );

            // We no longer look for the current process being dotnet because of Rider. Rider runs in dotnet.exe, but this
            // instance of dotnet.exe does not have an SDK installed. So, it is better to ignore the current process as a hint.

            // Check DOTNET_ROOT environment variables first.
            if ( this.TryGetDotNetRootFromEnvironment( out var dotnetRoot, out var variableName ) )
            {
                var dotnetPath = Path.Combine( dotnetRoot, dotnetFileName );

                if ( this._fileSystem.FileExists( dotnetPath ) )
                {
                    logger?.Trace?.Log( $"{dotnetFileName} found via {variableName}: '{dotnetPath}'." );

                    return dotnetPath;
                }
                else
                {
                    logger?.Trace?.Log( $"{variableName} is set to '{dotnetRoot}' but {dotnetFileName} was not found there." );
                }
            }

            // Search in default installation locations.
            foreach ( var directory in this.GetDefaultDotNetDirectories() )
            {
                var dotnetPath = Path.Combine( directory, dotnetFileName );

                if ( this._fileSystem.FileExists( dotnetPath ) )
                {
                    logger?.Trace?.Log( $"{dotnetFileName} found in default location '{dotnetPath}'." );

                    return dotnetPath;
                }
                else
                {
                    logger?.Trace?.Log( $"Looked for {dotnetFileName} in default location '{dotnetPath}' but it did not exist." );
                }
            }

            // Explicitly resolve PATH, because in the Rider process, "dotnet" alone would resolve to Rider's limited dotnet.
            // While doing so, ignore Rider's ReSharperHost paths, which contain that dotnet.

            var path = this._environmentVariableProvider.GetEnvironmentVariable( "PATH" );

            if ( path != null )
            {
                var splitCharacter = this._runtimeInformation.IsOSPlatform( OSPlatform.Windows ) ? ';' : ':';

                foreach ( var directory in path.Split( splitCharacter ) )
                {
                    if ( directory.ContainsOrdinal( "ReSharperHost" ) )
                    {
                        logger?.Trace?.Log( $"Rider directory '{directory}' excluded." );

                        continue;
                    }

                    var dotnetPath = Path.Combine( directory, dotnetFileName );

                    if ( this._fileSystem.FileExists( dotnetPath ) )
                    {
                        logger?.Trace?.Log( $"{dotnetFileName} found in '{dotnetPath}'." );

                        return dotnetPath;
                    }
                    else
                    {
                        logger?.Trace?.Log( $"Looked for {dotnetFileName} in '{dotnetPath}', but it did not exist." );
                    }
                }
            }

            // The file was not found.
            logger?.Trace?.Log( $"{dotnetFileName} was found nowhere. We hope it will be in the PATH." );

            return "dotnet";
        }

        /// <summary>
        /// Gets the default .NET installation directories for the current platform.
        /// Returns directories in priority order.
        /// Reference: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables
        /// </summary>
        /// <returns>An enumerable of directory paths to check for .NET installations.</returns>
        private IEnumerable<string> GetDefaultDotNetDirectories()
        {
            if ( this._runtimeInformation.IsOSPlatform( OSPlatform.Windows ) )
            {
                // On Windows, %ProgramFiles% expands to the correct directory for the current architecture.
                var programFiles = this._environmentVariableProvider.GetEnvironmentVariable( "ProgramFiles" );

                if ( programFiles != null )
                {
                    var baseDirectory = Path.Combine( programFiles, "dotnet" );

                    // On ARM64 OS running x64 process, check the x64 subfolder first.
                    if ( this._runtimeInformation.OSArchitecture == Architecture.Arm64 && this._runtimeInformation.ProcessArchitecture == Architecture.X64 )
                    {
                        yield return Path.Combine( baseDirectory, "x64" );
                    }

                    yield return baseDirectory;
                }
            }
            else if ( this._runtimeInformation.IsOSPlatform( OSPlatform.OSX ) )
            {
                var baseDirectory = "/usr/local/share/dotnet";

                // On ARM64 macOS running x64 process, check the x64 subfolder first.
                if ( this._runtimeInformation.OSArchitecture == Architecture.Arm64 && this._runtimeInformation.ProcessArchitecture == Architecture.X64 )
                {
                    yield return Path.Combine( baseDirectory, "x64" );
                }

                yield return baseDirectory;
            }
            else if ( this._runtimeInformation.IsOSPlatform( OSPlatform.Linux ) )
            {
                // Common locations on Linux (priority order).
                var linuxLocations = new[]
                {
                    "/usr/share/dotnet",  // packages.microsoft.com
                    "/usr/lib/dotnet"     // Ubuntu Jammy feed
                };

                // On ARM64 Linux running x64 process, check x64 subfolders first.
                if ( this._runtimeInformation.OSArchitecture == Architecture.Arm64 && this._runtimeInformation.ProcessArchitecture == Architecture.X64 )
                {
                    foreach ( var location in linuxLocations )
                    {
                        yield return Path.Combine( location, "x64" );
                    }
                }

                foreach ( var location in linuxLocations )
                {
                    yield return location;
                }
            }
        }

        /// <summary>
        /// Tries to get the .NET root directory from environment variables.
        /// Precedence order follows the official .NET SDK specification:
        /// https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables
        /// </summary>
        /// <param name="path">The directory path of the .NET root, if found.</param>
        /// <param name="variableName">The name of the environment variable that was matched.</param>
        /// <returns>True if a DOTNET_ROOT environment variable was found; otherwise, false.</returns>
        private bool TryGetDotNetRootFromEnvironment( [System.Diagnostics.CodeAnalysis.NotNullWhen( true )] out string? path, [System.Diagnostics.CodeAnalysis.NotNullWhen( true )] out string? variableName )
        {
            string? dotnetRoot;

            // 1. Check architecture-specific variables first (highest priority).
            switch ( this._runtimeInformation.ProcessArchitecture )
            {
                case Architecture.X64:
                    dotnetRoot = this._environmentVariableProvider.GetEnvironmentVariable( "DOTNET_ROOT_X64" );

                    if ( !string.IsNullOrEmpty( dotnetRoot ) )
                    {
                        path = dotnetRoot!;
                        variableName = "DOTNET_ROOT_X64";

                        return true;
                    }

                    break;

                case Architecture.X86:
                    dotnetRoot = this._environmentVariableProvider.GetEnvironmentVariable( "DOTNET_ROOT_X86" );

                    if ( !string.IsNullOrEmpty( dotnetRoot ) )
                    {
                        path = dotnetRoot!;
                        variableName = "DOTNET_ROOT_X86";

                        return true;
                    }

                    // 2. For x86 on 64-bit Windows, check DOTNET_ROOT(x86) next.
                    if ( this._runtimeInformation.IsOSPlatform( OSPlatform.Windows ) )
                    {
                        dotnetRoot = this._environmentVariableProvider.GetEnvironmentVariable( "DOTNET_ROOT(x86)" );

                        if ( !string.IsNullOrEmpty( dotnetRoot ) )
                        {
                            path = dotnetRoot!;
                            variableName = "DOTNET_ROOT(x86)";

                            return true;
                        }
                    }

                    break;

                case Architecture.Arm64:
                    dotnetRoot = this._environmentVariableProvider.GetEnvironmentVariable( "DOTNET_ROOT_ARM64" );

                    if ( !string.IsNullOrEmpty( dotnetRoot ) )
                    {
                        path = dotnetRoot!;
                        variableName = "DOTNET_ROOT_ARM64";

                        return true;
                    }

                    break;
            }

            // 3. Check generic DOTNET_ROOT as fallback (lowest priority).
            dotnetRoot = this._environmentVariableProvider.GetEnvironmentVariable( "DOTNET_ROOT" );

            if ( !string.IsNullOrEmpty( dotnetRoot ) )
            {
                path = dotnetRoot!;
                variableName = "DOTNET_ROOT";

                return true;
            }

            path = null;
            variableName = null;

            return false;
        }
    }
}