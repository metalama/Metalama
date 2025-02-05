// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.Infrastructure
{
    internal sealed class PlatformInfo : IPlatformInfo
    {
        public string? DotNetSdkDirectory { get; }

        public string DotNetExePath { get; }

        public string? DotNetSdkVersion { get; }

        public PlatformInfo( IServiceProvider serviceProvider, string? dotNetSdkDirectory )
        {
            var environmentVariableProvider = serviceProvider.GetRequiredBackstageService<IEnvironmentVariableProvider>();

            this.DotNetSdkDirectory = dotNetSdkDirectory
                                      ?? environmentVariableProvider.GetEnvironmentVariable( _dotNetSdkDirectoryEnvironmentVariableName );

            var logger = serviceProvider.GetLoggerFactory().GetLogger( "PlatformInfo" );

            this.DotNetSdkVersion = Path.GetFileName( this.DotNetSdkDirectory );
            this.DotNetExePath = GetDotNetPath( logger, this.DotNetSdkDirectory );
        }

        private const string _dotNetSdkDirectoryEnvironmentVariableName = "MSBuildExtensionsPath";

        private static string GetDotNetPath( ILogger logger, string? dotNetSdkDirectory = null )
        {
            var dotnetFileName = RuntimeInformation.IsOSPlatform( OSPlatform.Windows )
                ? "dotnet.exe"
                : "dotnet";

            logger.Trace?.Log( $"Looking for {dotnetFileName} path." );

            // We no longer look for the current process being dotnet because of Rider. Rider runs in dotnet.exe, but this
            // instance of dotnet.exe does not have an SDK installed. So, it is better to ignore the current process as a hint.

            // Look in the DotNetSdkDirectory, if we know it.
            dotNetSdkDirectory ??= Environment.GetEnvironmentVariable( _dotNetSdkDirectoryEnvironmentVariableName );

            if ( !string.IsNullOrEmpty( dotNetSdkDirectory ) )
            {
                for ( var directory = dotNetSdkDirectory; directory != null; directory = Path.GetDirectoryName( directory ) )
                {
                    var dotnetPath = Path.Combine( directory, dotnetFileName );

                    if ( File.Exists( dotnetPath ) )
                    {
                        logger.Trace?.Log( $"{dotnetFileName} found in '{dotnetPath}'." );

                        return dotnetPath;
                    }
                    else
                    {
                        logger.Trace?.Log( $"Looked for {dotnetFileName} in '{dotnetPath}', but it did not exist." );
                    }
                }
            }

            // Search dotnet.exe in well-known locations.
            if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
            {
                // %ProgramFiles% is expanded to the correct directory according to the current processor architecture.
                // This is good because we always want to get dotnet.exe for the current processor architecture.
                var dotnetPath = Environment.ExpandEnvironmentVariables( "%ProgramFiles%\\dotnet\\dotnet.exe" );

                if ( File.Exists( dotnetPath ) )
                {
                    logger.Trace?.Log( $"dotnet.exe found in '{dotnetPath}'." );

                    return dotnetPath;
                }
                else
                {
                    logger.Trace?.Log( $"Looked for dotnet.exe in '{dotnetPath}' but it did not exist." );
                }
            }

            // Explicitly resolve PATH, because in the Rider process, "dotnet" alone would resolve to Rider's limited dotnet.
            // While doing so, ignore Rider's ReSharperHost paths, which contain that dotnet.

            var path = Environment.GetEnvironmentVariable( "PATH" );

            if ( path != null )
            {
                var splitCharacter = RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ? ';' : ':';

                foreach ( var directory in path.Split( splitCharacter ) )
                {
                    if ( directory.ContainsOrdinal( "ReSharperHost" ) )
                    {
                        logger.Trace?.Log( $"Rider directory '{directory}' excluded." );

                        continue;
                    }

                    var dotnetPath = Path.Combine( directory, dotnetFileName );

                    if ( File.Exists( dotnetPath ) )
                    {
                        logger.Trace?.Log( $"{dotnetFileName} found in '{dotnetPath}'." );

                        return dotnetPath;
                    }
                    else
                    {
                        logger.Trace?.Log( $"Looked for {dotnetFileName} in '{dotnetPath}', but it did not exist." );
                    }
                }
            }

            // The file was not found.
            logger.Trace?.Log( $"{dotnetFileName} was found nowhere. We hope it will be in the PATH." );

            return "dotnet";
        }
    }
}