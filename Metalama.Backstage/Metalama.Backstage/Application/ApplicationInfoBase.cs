// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Utilities;
using System.Collections.Immutable;
using System.Reflection;
using ILoggerFactory = Metalama.Backstage.Diagnostics.ILoggerFactory;

namespace Metalama.Backstage.Application
{
    /// <summary>
    /// Implementation of <see cref="IApplicationInfo" /> interface with build information
    /// initialized from assembly metadata using <see cref="AssemblyMetadataReader" />.
    /// </summary>
    public abstract class ApplicationInfoBase : ComponentInfoBase, IApplicationInfo
    {
        protected ApplicationInfoBase( Assembly metadataAssembly ) : base( metadataAssembly )
        {
            if ( this.PackageVersion != null )
            {
                this.IsTelemetryEnabled = !VersionHelper.IsDevelopmentVersion( this.PackageVersion );
            }
        }

        /// <inheritdoc />
        public virtual ProcessKind ProcessKind => ProcessUtilities.ProcessKind;

        /// <inheritdoc />
        public virtual bool IsLongRunningProcess => false;

        /// <inheritdoc />
        public virtual bool IsUnattendedProcess( ILoggerFactory loggerFactory ) => ProcessUtilities.IsCurrentProcessUnattended( loggerFactory );

        /// <inheritdoc />
        public virtual bool IsTelemetryEnabled { get; }

        /// <inheritdoc />
        public virtual bool IsLicenseAuditEnabled => false;

        /// <inheritdoc />
        public virtual bool ShouldCreateLocalCrashReports => true;

        public virtual ImmutableArray<IComponentInfo> Components => ImmutableArray<IComponentInfo>.Empty;
    }
}