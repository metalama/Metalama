// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.Infrastructure
{
    // We base this class on
    // https://enbravikov.wordpress.com/2018/09/15/special-folder-enum-values-on-windows-and-linux-ubuntu-16-04-in-net-core/
    // but not all platforms respect this. For such platforms, we provide fallbacks.

    /// <inheritdoc />
    internal sealed class StandardDirectories : IStandardDirectories
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRuntimeInformation _runtimeInformation;
        private readonly string? _overriddenTempPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardDirectories"/> class.
        /// </summary>
        public StandardDirectories( IServiceProvider serviceProvider )
        {
            // Until .NET 8.0, ApplicationData and LocalApplication directories followed the same mechanism as Linux, i.e.:
            //   * ApplicationData: $XDG_CONFIG_HOME, or $HOME/.config
            //   * LocalApplicationData: $XDG_DATA_HOME, or $HOME/.local/share
            // From .NET 8.0 onward, these directories both point to $HOME/Library/Application Support.

            // Since Metalama.Tool is a .NET 6.0 application with "rollover": "major", it's behavior depended on installed runtimes.
            // When both .NET 6.0 and .NET 8.0 runtimes were installed, the compiler would run on .NET 8 and the tool on .NET 6,
            // which leads to accessing different directories in each part.

            this._serviceProvider = serviceProvider;
            this._runtimeInformation = serviceProvider.GetRequiredBackstageService<IRuntimeInformation>();

            var overriddenTempPath = serviceProvider.GetRequiredBackstageService<IEnvironmentVariableProvider>().GetEnvironmentVariable( "METALAMA_TEMP" );
            this._overriddenTempPath = string.IsNullOrEmpty( overriddenTempPath ) ? null : overriddenTempPath;

            var logger = serviceProvider.GetBackstageService<EarlyLoggerFactory>()?.GetLogger( nameof(StandardDirectories) );

            static string GetApplicationDataDirectory( Environment.SpecialFolder applicationDataDirectory, string metalamaDirectoryName )
            {
                var applicationDataParentDirectory = Environment.GetFolderPath( applicationDataDirectory );

                if ( !string.IsNullOrEmpty( applicationDataParentDirectory ) )
                {
                    // Normalize the path to ensure consistent path separators.
                    return Path.GetFullPath( Path.Combine( applicationDataParentDirectory, metalamaDirectoryName ) );
                }
                else
                {
                    // This is a fallback for Ubuntu on WSL and other platforms that don't provide
                    // the SpecialFolder.ApplicationData folder path.
                    applicationDataParentDirectory = Environment.GetFolderPath( Environment.SpecialFolder.UserProfile );

                    if ( string.IsNullOrEmpty( applicationDataParentDirectory ) )
                    {
                        // This will always fail on platforms which don't provide the special folders being discovered above.
                        // We need to find another locations on such platforms.
                        throw new InvalidOperationException( "Failed to find application data parent directory." );
                    }

                    // We use the name ".metalama" here, because in this case the settings go to the user's home directory.
                    // Normalize the path to ensure consistent path separators.
                    return Path.GetFullPath( Path.Combine( applicationDataParentDirectory, ".metalama" ) );
                }
            }

            // Till Metalama Backstage 2024.1.8, the application data directory was incorrect.
            var incorrectApplicationDataDirectory = GetApplicationDataDirectory( Environment.SpecialFolder.ApplicationData, ".metalama" );
            var correctApplicationDataDirectory = GetApplicationDataDirectory( Environment.SpecialFolder.LocalApplicationData, "Metalama" );

            // On OSX, when running on .NET 6.0 or 7.0, we need to check that the standard directory returned on .NET 8 is not used, in which case we should use it.
            // This makes sure that that we are not using a different directory when mixing .NET 6.0 and .NET 8.0.
            var osxForwardCompatibleApplicationDataDirectory =
                RuntimeInformation.IsOSPlatform( OSPlatform.OSX ) && Environment.Version < new Version( 8, 0 )
                    ? Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), "Library", "Application Support", "Metalama" )
                    : null;

            if ( Directory.Exists( incorrectApplicationDataDirectory ) )
            {
                // In case the incorrect directory exists already, we won't start using the correct one.
                logger?.Warning?.Log(
                    $"Using obsolete data directory '{incorrectApplicationDataDirectory}', " +
                    $"please migrate the content to '{osxForwardCompatibleApplicationDataDirectory ?? correctApplicationDataDirectory}'." );

                this.ApplicationDataDirectory = incorrectApplicationDataDirectory;
            }
            else
            {
                if ( osxForwardCompatibleApplicationDataDirectory != null
                     && osxForwardCompatibleApplicationDataDirectory != correctApplicationDataDirectory )
                {
                    // This runs only on OSX and .NET 6.0 or 7.0.

                    switch (Directory.Exists( correctApplicationDataDirectory ), Directory.Exists( osxForwardCompatibleApplicationDataDirectory ))
                    {
                        case (false, _):
                            // The .NET 6 directory does not exists, we use the forward-compatible one.
                            logger?.Trace?.Log( $"Using forward-compatible data directory '{osxForwardCompatibleApplicationDataDirectory}'." );
                            this.ApplicationDataDirectory = osxForwardCompatibleApplicationDataDirectory;

                            return;

                        case (true, true):
                            // Both directories exist. We choose the forward-compatible one and warn the user.
                            logger?.Warning?.Log(
                                $"Using forward-compatible data directory '{osxForwardCompatibleApplicationDataDirectory}' " +
                                $"even though '{correctApplicationDataDirectory}' already exists." +
                                $"This indicates mixing of .NET runtimes before and after .NET 8.0 in older versions of Metalama. " +
                                $"Please inspect the latter directory, migrate it's content to the former directory." );

                            this.ApplicationDataDirectory = osxForwardCompatibleApplicationDataDirectory;

                            return;
                    }
                }

                logger?.Trace?.Log( $"Using data directory '{correctApplicationDataDirectory}' " );
                this.ApplicationDataDirectory = correctApplicationDataDirectory;
            }
        }

        /// <inheritdoc />
        public string ApplicationDataDirectory { get; }

        // The Metalama temp directory holds assemblies that Metalama loads and executes, so it must be writable only by
        // the current user. On Unix, Path.GetTempPath() (/tmp) is world-writable and would allow DLL planting (#1650),
        // so we use the per-user application-data directory there. On Windows the temp directory is already per-user.
        // The OS and environment are read through injected services so both branches are unit-testable on any platform.

        /// <inheritdoc />
        public string TempDirectory
        {
            get
            {
                if ( this._overriddenTempPath != null )
                {
                    return Path.Combine( this._overriddenTempPath, "Metalama" );
                }

                if ( this._runtimeInformation.IsOSPlatform( OSPlatform.Windows ) )
                {
                    return Path.Combine( Path.GetTempPath(), "Metalama" );
                }

                return Path.Combine( this.ApplicationDataDirectory, "Temp" );
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<string> LegacyTempDirectories
            => this._overriddenTempPath != null || this._runtimeInformation.IsOSPlatform( OSPlatform.Windows )
                ? Array.Empty<string>()

                // The pre-#1650 location on Unix, e.g. '/tmp/Metalama'.
                : new[] { Path.Combine( Path.GetTempPath(), "Metalama" ) };

        /// <inheritdoc />
        public string TelemetryLogsDirectory => Path.Combine( this.ApplicationDataDirectory, "Telemetry", "Logs" );

        /// <inheritdoc />
        public string TelemetryExceptionsDirectory => Path.Combine( this.ApplicationDataDirectory, "Telemetry", "Exceptions" );

        /// <inheritdoc />
        public string TelemetryUploadQueueDirectory => Path.Combine( this.ApplicationDataDirectory, "Telemetry", "UploadQueue" );

        /// <inheritdoc />
        public string TelemetryUploadPackagesDirectory => Path.Combine( this.ApplicationDataDirectory, "Telemetry", "Packages" );

        public string CrashReportsDirectory
            => this._serviceProvider.GetRequiredBackstageService<ITempFileManager>()
                .GetTempDirectory( "CrashReports", CleanUpStrategy.FileOneMonthAfterCreation, versionScope: TempFileVersionScope.None );
    }
}