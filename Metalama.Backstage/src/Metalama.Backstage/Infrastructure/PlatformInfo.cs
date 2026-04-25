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

        /// <summary>
        /// Checks whether a dotnet.exe at the given path has an SDK installation (i.e. a <c>sdk</c> subdirectory
        /// next to the executable). Some hosts (e.g. Visual Studio, Rider) bundle a runtime-only dotnet.exe
        /// that has no SDK, which would cause <c>dotnet build</c> to fail.
        /// </summary>
        private bool HasSdk( string dotnetExePath, ILogger? logger )
        {
            var dotnetDir = Path.GetDirectoryName( dotnetExePath );
            var sdkDir = dotnetDir != null ? Path.Combine( dotnetDir, "sdk" ) : null;

            if ( sdkDir != null && this._fileSystem.DirectoryExists( sdkDir ) )
            {
                return true;
            }

            logger?.Warning?.Log( $"'{dotnetExePath}' exists but its installation has no SDK directory. Ignoring." );

            return false;
        }

        private string GetDotNetPath()
        {
            var logger = this._serviceProvider.GetBackstageService<EarlyLoggerFactory>()?.GetLogger( "PlatformInfo" );

            var dotnetFileName = this._runtimeInformation.IsOSPlatform( OSPlatform.Windows )
                ? "dotnet.exe"
                : "dotnet";

            logger?.Trace?.Log( $"Looking for {dotnetFileName} path." );

            // 1. Check DOTNET_HOST_PATH first (highest priority).
            // This is the absolute path to the dotnet host executable.
            {
                var dotnetHostPath = this._environmentVariableProvider.GetEnvironmentVariable( "DOTNET_HOST_PATH" );

                if ( !string.IsNullOrEmpty( dotnetHostPath ) )
                {
                    if ( this._fileSystem.FileExists( dotnetHostPath ) && this.HasSdk( dotnetHostPath, logger ) )
                    {
                        logger?.Trace?.Log( $"{dotnetFileName} found via DOTNET_HOST_PATH: '{dotnetHostPath}'." );

                        return dotnetHostPath;
                    }
                    else if ( !this._fileSystem.FileExists( dotnetHostPath ) )
                    {
                        logger?.Warning?.Log( $"DOTNET_HOST_PATH is set to '{dotnetHostPath}' but the file was not found." );
                    }
                }
            }

            // 2. Search in installation locations (DOTNET_ROOT env vars, then default locations).
            foreach ( var directory in this.GetDotNetDirectories() )
            {
                var dotnetPath = Path.Combine( directory, dotnetFileName );

                if ( this._fileSystem.FileExists( dotnetPath ) && this.HasSdk( dotnetPath, logger ) )
                {
                    logger?.Trace?.Log( $"{dotnetFileName} found in '{dotnetPath}'." );

                    return dotnetPath;
                }
                else if ( !this._fileSystem.FileExists( dotnetPath ) )
                {
                    logger?.Trace?.Log( $"Looked for {dotnetFileName} in '{dotnetPath}' but it did not exist." );
                }
            }

            // 3. Explicitly resolve PATH.
            var path = this._environmentVariableProvider.GetEnvironmentVariable( "PATH" );

            if ( path != null )
            {
                var splitCharacter = this._runtimeInformation.IsOSPlatform( OSPlatform.Windows ) ? ';' : ':';

                foreach ( var directory in path.Split( splitCharacter ) )
                {
                    var dotnetPath = Path.Combine( directory, dotnetFileName );

                    if ( this._fileSystem.FileExists( dotnetPath ) && this.HasSdk( dotnetPath, logger ) )
                    {
                        logger?.Trace?.Log( $"{dotnetFileName} found in '{dotnetPath}'." );

                        return dotnetPath;
                    }
                    else if ( !this._fileSystem.FileExists( dotnetPath ) )
                    {
                        logger?.Trace?.Log( $"Looked for {dotnetFileName} in '{dotnetPath}', but it did not exist." );
                    }
                }
            }

            // The file was not found.
            logger?.Warning?.Log( $"{dotnetFileName} was found nowhere. We hope it will be in the PATH." );

            return "dotnet";
        }

        /// <summary>
        /// Gets the .NET installation directories for the current platform.
        /// Returns directories in priority order: first based on DOTNET_ROOT environment variables, then on default locations.
        /// Note: DOTNET_HOST_PATH is checked separately before this method, as it specifies the full path to the executable.
        /// Reference: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables
        /// </summary>
        /// <returns>A list of directory paths to check for .NET installations.</returns>
        private IEnumerable<string> GetDotNetDirectories()
        {
            foreach ( var environmentVariableName in this.GetDotNetRootEnvironmentVariables() )
            {
                var environmentVariableValue = this._environmentVariableProvider.GetEnvironmentVariable( environmentVariableName );

                if ( !string.IsNullOrWhiteSpace( environmentVariableValue ) )
                {
                    // ReSharper disable once RedundantSuppressNullableWarningExpression
                    yield return environmentVariableValue!;
                }
            }

            if ( this._runtimeInformation.IsOSPlatform( OSPlatform.Windows ) )
            {
                // On Windows, %ProgramFiles% expands to the correct directory for the current architecture.
                var programFiles = this._environmentVariableProvider.GetEnvironmentVariable( "ProgramFiles" );

                if ( programFiles != null )
                {
                    var baseDirectory = Path.Combine( programFiles, "dotnet" );

                    // On ARM64 OS running x64 process, check the x64 subfolder first.
                    if ( this._runtimeInformation is { OSArchitecture: Architecture.Arm64, ProcessArchitecture: Architecture.X64 } )
                    {
                        yield return Path.Combine( baseDirectory, "x64" );
                    }

                    yield return baseDirectory;
                }
            }
            else if ( this._runtimeInformation.IsOSPlatform( OSPlatform.OSX ) )
            {
                const string baseDirectory = "/usr/local/share/dotnet";

                // On ARM64 macOS running x64 process, check the x64 subfolder first.
                if ( this._runtimeInformation is { OSArchitecture: Architecture.Arm64, ProcessArchitecture: Architecture.X64 } )
                {
                    yield return Path.Combine( baseDirectory, "x64" );
                }

                yield return baseDirectory;
            }
            else if ( this._runtimeInformation.IsOSPlatform( OSPlatform.Linux ) )
            {
                // Common locations on Linux (priority order).
                string[] linuxLocations =
                [
                    "/usr/share/dotnet", // packages.microsoft.com
                    "/usr/lib/dotnet"    // Ubuntu Jammy feed
                ];

                // On ARM64 Linux running x64 process, check x64 subfolders first.
                if ( this._runtimeInformation is { OSArchitecture: Architecture.Arm64, ProcessArchitecture: Architecture.X64 } )
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
        /// Gets the DOTNET_ROOT environment variable names to check in priority order.
        /// Precedence order follows the official .NET SDK specification:
        /// https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables
        /// </summary>
        /// <returns>An enumerable of environment variable names in priority order.</returns>
        private IEnumerable<string> GetDotNetRootEnvironmentVariables()
        {
            // 1. Check architecture-specific variables first (highest priority).
            switch ( this._runtimeInformation.ProcessArchitecture )
            {
                case Architecture.X64:
                    yield return "DOTNET_ROOT_X64";

                    break;

                case Architecture.X86:
                    yield return "DOTNET_ROOT_X86";

                    // 2. For x86 on 64-bit Windows, check DOTNET_ROOT(x86) next.
                    if ( this._runtimeInformation.IsOSPlatform( OSPlatform.Windows ) )
                    {
                        yield return "DOTNET_ROOT(x86)";
                    }

                    break;

                case Architecture.Arm64:
                    yield return "DOTNET_ROOT_ARM64";

                    break;
            }

            // 3. Check generic DOTNET_ROOT as fallback (lowest priority).
            yield return "DOTNET_ROOT";
        }
    }
}