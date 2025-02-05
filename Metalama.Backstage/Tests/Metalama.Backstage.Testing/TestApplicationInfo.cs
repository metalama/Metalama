// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Utilities;
using System;
using System.Collections.Immutable;

namespace Metalama.Backstage.Testing
{
    // This object must be immutable because implementations except any IApplicationInfo to be immutable.
    public class TestApplicationInfo : IApplicationInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestApplicationInfo"/> class.
        /// </summary>
        /// <param name="isPrerelease">A value indicating whether the application is a pre-release.</param>
        /// <param name="version">The version of the application.</param>
        /// <param name="buildDate">The date of build of the application.</param>
        public TestApplicationInfo(
            string name,
            bool isPrerelease,
            string version,
            DateTime buildDate )
        {
            this.Name = name;
            this.IsPrerelease = isPrerelease;
            this.PackageVersion = version;
            this.BuildDate = buildDate;
            this.Company = AssemblyMetadataReader.GetInstance( typeof(TestApplicationInfo).Assembly ).Company;
        }

        public TestApplicationInfo() : this( "test", false, "0.0", DateTime.Now ) { }

        public string? Company { get; init; }

        /// <inheritdoc />
        public string Name { get; }

        public Version? AssemblyVersion => TestVersionHelper.GetAssemblyVersionFromPackageVersion( this.PackageVersion );

        /// <inheritdoc />
        public bool? IsPrerelease { get; init; }

        /// <inheritdoc />
        public string? PackageVersion { get; }

        /// <inheritdoc />
        public DateTime? BuildDate { get; }

        /// <inheritdoc />
        public ProcessKind ProcessKind => ProcessKind.Other;

        public bool IsUnattendedProcess { get; init; }

        /// <inheritdoc />
        bool IApplicationInfo.IsUnattendedProcess( ILoggerFactory loggerFactory ) => this.IsUnattendedProcess;

        /// <inheritdoc />
        public bool IsLongRunningProcess { get; init; }

        /// <inheritdoc />
        public bool IsTelemetryEnabled { get; init; }

        /// <inheritdoc />
        public bool IsLicenseAuditEnabled { get; init; } = true;

        /// <inheritdoc />
        public bool ShouldCreateLocalCrashReports => this.IsTelemetryEnabled;

        /// <inheritdoc />
        public ImmutableArray<IComponentInfo> Components { get; init; } = ImmutableArray<IComponentInfo>.Empty;
    }
}