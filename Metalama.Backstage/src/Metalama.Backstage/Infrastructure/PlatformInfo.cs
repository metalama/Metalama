// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Utilities;
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
        private readonly IProcessInfo _processInfo;
        private readonly Lazy<string> _dotNetExePath;
        private readonly Lazy<bool> _isRunningUnderRider;

        public string DotNetExePath => this._dotNetExePath.Value;

        public PlatformInfo( IServiceProvider serviceProvider )
        {
            this._serviceProvider = serviceProvider;
            this._environmentVariableProvider = serviceProvider.GetRequiredBackstageService<IEnvironmentVariableProvider>();
            this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
            this._runtimeInformation = serviceProvider.GetRequiredBackstageService<IRuntimeInformation>();
            this._processInfo = serviceProvider.GetRequiredBackstageService<IProcessInfo>();

            this._isRunningUnderRider = new Lazy<bool>( this.DetectRider );
            this._dotNetExePath = new Lazy<string>( this.GetDotNetPath );
        }

        /// <summary>
        /// Detects if the current process is running under JetBrains Rider.
        /// Rider runs in its own dotnet.exe instance and sets DOTNET_ROOT to point to its limited installation,
        /// which doesn't have the SDK we need. We need to skip environment variable detection in this case.
        /// </summary>
        private bool DetectRider()
        {
            return this._processInfo.ProcessKind == ProcessKind.Rider;
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

            // Check if we're running under Rider.
            var isRunningUnderRider = this._isRunningUnderRider.Value;

            if ( isRunningUnderRider )
            {
                logger?.Trace?.Log(
                    "Detected JetBrains Rider environment (ReSharperHost found in PATH). Skipping DOTNET_HOST_PATH and DOTNET_ROOT environment variables to avoid using Rider's limited dotnet installation." );
            }

            // 1. Check DOTNET_HOST_PATH first (highest priority), unless running under Rider.
            // This is the absolute path to the dotnet host executable.
            if ( !isRunningUnderRider )
            {
                var dotnetHostPath = this._environmentVariableProvider.GetEnvironmentVariable( "DOTNET_HOST_PATH" );

                if ( !string.IsNullOrEmpty( dotnetHostPath ) )
                {
                    if ( this._fileSystem.FileExists( dotnetHostPath ) )
                    {
                        logger?.Trace?.Log( $"{dotnetFileName} found via DOTNET_HOST_PATH: '{dotnetHostPath}'." );

                        return dotnetHostPath;
                    }
                    else
                    {
                        logger?.Warning?.Log( $"DOTNET_HOST_PATH is set to '{dotnetHostPath}' but the file was not found." );
                    }
                }
            }

            // 2. Search in installation locations.
            foreach ( var directory in this.GetDotNetDirectories() )
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
            logger?.Warning?.Log( $"{dotnetFileName} was found nowhere. We hope it will be in the PATH." );

            return "dotnet";
        }

        /// <summary>
        /// Gets the .NET installation directories for the current platform.
        /// Returns directories in priority order: first based on DOTNET_ROOT environment variables (unless running under Rider), then on default locations.
        /// Note: DOTNET_HOST_PATH is checked separately before this method, as it specifies the full path to the executable (also skipped when running under Rider).
        /// Reference: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables
        /// </summary>
        /// <returns>A list of directory paths to check for .NET installations.</returns>
        private IEnumerable<string> GetDotNetDirectories()
        {
            // Skip DOTNET_ROOT environment variables when running under Rider, as Rider overrides them
            // to point to its own limited dotnet installation which doesn't have the SDK we need.
            // (DOTNET_HOST_PATH is also skipped when running under Rider - see GetDotNetPath method.)
            if ( !this._isRunningUnderRider.Value )
            {
                foreach ( var environmentVariableName in this.GetDotNetRootEnvironmentVariables() )
                {
                    var environmentVariableValue = this._environmentVariableProvider.GetEnvironmentVariable( environmentVariableName );

                    if ( !string.IsNullOrWhiteSpace( environmentVariableValue ) )
                    {
                        yield return environmentVariableValue!;
                    }
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
                    if ( this._runtimeInformation.OSArchitecture == Architecture.Arm64 && this._runtimeInformation.ProcessArchitecture == Architecture.X64 )
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
                if ( this._runtimeInformation.OSArchitecture == Architecture.Arm64 && this._runtimeInformation.ProcessArchitecture == Architecture.X64 )
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