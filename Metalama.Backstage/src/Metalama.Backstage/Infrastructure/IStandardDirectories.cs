// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System.Collections.Generic;

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
        /// Gets the path of the current user's application temporary folder. Unless overridden by the
        /// <c>METALAMA_TEMP</c> environment variable, on Unix this lives under <see cref="ApplicationDataDirectory"/>
        /// and not under the world-writable <c>/tmp</c> (issue #1650), because this directory holds assemblies
        /// that Metalama loads and executes.
        /// </summary>
        string TempDirectory { get; }

        /// <summary>
        /// Gets temp directories used by previous versions of Metalama but no longer used by this one, so that the
        /// cache clean-up can remove them. Empty unless the temp location has changed across versions.
        /// </summary>
        IReadOnlyList<string> LegacyTempDirectories { get; }

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