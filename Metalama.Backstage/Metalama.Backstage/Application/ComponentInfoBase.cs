// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Utilities;
using System;
using System.Globalization;
using System.Reflection;

namespace Metalama.Backstage.Application;

public abstract class ComponentInfoBase : IComponentInfo
{
    protected ComponentInfoBase( Assembly metadataAssembly )
    {
        var reader = AssemblyMetadataReader.GetInstance( metadataAssembly );
        this.PackageVersion = reader.PackageVersion;
        this.AssemblyVersion = reader.AssemblyVersion;

        // IsPrerelease flag can be overridden for testing purposes.
        var isPrereleaseEnvironmentVariableValue = Environment.GetEnvironmentVariable( "METALAMA_IS_PRERELEASE" );
        bool? isPrereleaseOverriddenValue = isPrereleaseEnvironmentVariableValue == null ? null : bool.Parse( isPrereleaseEnvironmentVariableValue );
        this.IsPrerelease = isPrereleaseOverriddenValue ?? (this.PackageVersion != null && VersionHelper.IsPrereleaseVersion( this.PackageVersion ));

        // BuildDate value can be overridden for testing purposes.
        var buildDateEnvironmentVariableValue = Environment.GetEnvironmentVariable( "METALAMA_BUILD_DATE" );

        DateTime? buildDateOverriddenValue = buildDateEnvironmentVariableValue == null
            ? null
            : DateTime.Parse( buildDateEnvironmentVariableValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal );

        this.BuildDate = buildDateOverriddenValue ?? reader.BuildDate;
        this.Company = reader.Company;
    }

    /// <inheritdoc />
    public string? Company { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public string? PackageVersion { get; }

    public Version? AssemblyVersion { get; }

    /// <inheritdoc />
    public bool? IsPrerelease { get; }

    /// <inheritdoc />
    public DateTime? BuildDate { get; }
}