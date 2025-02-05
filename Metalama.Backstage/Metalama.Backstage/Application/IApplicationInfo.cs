// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using System;
using System.Collections.Immutable;

namespace Metalama.Backstage.Application
{
    /// <summary>
    /// Provides version information about an application.
    /// </summary>
    public interface IApplicationInfo : IComponentInfo
    {
        ProcessKind ProcessKind { get; }

        /// <summary>
        /// Determines whether the current process is unattended. This method accepts an <see cref="ILoggerFactory"/> because
        /// the <see cref="IServiceProvider"/> cannot be available when we instantiate implementations of this interface.
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        bool IsUnattendedProcess( ILoggerFactory loggerFactory );

        bool IsLongRunningProcess { get; }

        /// <summary>
        /// Gets a value indicating whether telemetry is enabled for the application.
        /// </summary>
        bool IsTelemetryEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether licenses should be audited by this application.
        /// </summary>
        bool IsLicenseAuditEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether crashes should be reported for the application.
        /// </summary>
        bool ShouldCreateLocalCrashReports { get; }

        /// <summary>
        /// Gets the list of additional components of the application.
        /// </summary>
        ImmutableArray<IComponentInfo> Components { get; }
    }
}