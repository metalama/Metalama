// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Utilities;
using System;
using System.IO;

namespace Metalama.Backstage.Infrastructure
{
    // We base this class on
    // https://enbravikov.wordpress.com/2018/09/15/special-folder-enum-values-on-windows-and-linux-ubuntu-16-04-in-net-core/
    // but not all platforms respect this. For such platforms, we provide fallbacks.

    /// <inheritdoc />
    internal class StandardDirectories : IStandardDirectories
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardDirectories"/> class.
        /// </summary>
        public StandardDirectories( IServiceProvider serviceProvider )
        {
            this._serviceProvider = serviceProvider;

            static string GetApplicationDataDirectory( Environment.SpecialFolder applicationDataDirectory, string metalamaDirectoryName )
            {
                var applicationDataParentDirectory = Environment.GetFolderPath( applicationDataDirectory );

                if ( !string.IsNullOrEmpty( applicationDataParentDirectory ) )
                {
                    return Path.Combine( applicationDataParentDirectory, metalamaDirectoryName );
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
                    return Path.Combine( applicationDataParentDirectory, ".metalama" );
                }
            }

            // Till Metalama Backstage 2024.1.8, the application data directory was incorrect.
            var incorrectApplicationDataDirectory = GetApplicationDataDirectory( Environment.SpecialFolder.ApplicationData, ".metalama" );

            if ( Directory.Exists( incorrectApplicationDataDirectory ) )
            {
                // In case the incorrect directory exists already, we won't start using the correct one.
                this.ApplicationDataDirectory = incorrectApplicationDataDirectory;
            }
            else
            {
                var correctApplicationDataDirectory = GetApplicationDataDirectory( Environment.SpecialFolder.LocalApplicationData, "Metalama" );
                this.ApplicationDataDirectory = correctApplicationDataDirectory;
            }
        }

        /// <inheritdoc />
        public string ApplicationDataDirectory { get; }

        /// <inheritdoc />
        public string TempDirectory { get; } = Path.Combine( MetalamaPathUtilities.GetTempPath(), "Metalama" );

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