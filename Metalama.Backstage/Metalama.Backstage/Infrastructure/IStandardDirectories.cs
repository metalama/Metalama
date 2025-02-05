// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Infrastructure
{
    /// <summary>
    /// Provides paths of standard directories.
    /// </summary>
    public interface IStandardDirectories : IBackstageService
    {
        /// <summary>
        /// Gets the directory that serves as a common repository for application-specific data for the current roaming user.
        /// </summary>
        string ApplicationDataDirectory { get; }

        /// <summary>
        /// Gets the path of the current user's application temporary folder.
        /// </summary>
        string TempDirectory { get; }

        /// <summary>
        /// Gets the directory where telemetry logs are written.
        /// </summary>
        string TelemetryLogsDirectory { get; }

        /// <summary>
        /// Gets the directory where the exception reports should be stored just after they are captured.
        /// </summary>
        string TelemetryExceptionsDirectory { get; }

        /// <summary>
        /// Gets the directory where files to be packed for sending to the server have to be stored.
        /// </summary>
        string TelemetryUploadQueueDirectory { get; }

        /// <summary>
        /// Gets the directory where package files to be uploaded to the server have to be stored.
        /// </summary>
        string TelemetryUploadPackagesDirectory { get; }

        string CrashReportsDirectory { get; }
    }
}