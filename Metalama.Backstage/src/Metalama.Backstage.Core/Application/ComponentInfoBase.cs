// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Utilities;
using System;
using System.Globalization;
using System.Reflection;

namespace Metalama.Backstage.Application;

public abstract class ComponentInfoBase : IComponentInfo
{
    /// <summary>
    /// The prefix of the environment variables read by the constructor that takes no <see cref="ProductProfile"/>.
    /// It is the prefix of the product family that existed before the profile was introduced, so that the hosts
    /// written before the profile keep reading the same variables.
    /// </summary>
    private const string _compatibilityEnvironmentVariablePrefix = "METALAMA_";

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentInfoBase"/> class for a component of a host that was
    /// written before the product profile was introduced. The build-time overrides are read from the
    /// <c>METALAMA_IS_PRERELEASE</c> and <c>METALAMA_BUILD_DATE</c> environment variables.
    /// </summary>
    /// <param name="metadataAssembly">The assembly whose metadata describes the component.</param>
    /// <remarks>
    /// A host of another product family uses the constructor that takes a <see cref="ProductProfile"/>.
    /// </remarks>
    protected ComponentInfoBase( Assembly metadataAssembly ) : this( metadataAssembly, _compatibilityEnvironmentVariablePrefix ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentInfoBase"/> class.
    /// </summary>
    /// <param name="metadataAssembly">The assembly whose metadata describes the component.</param>
    /// <param name="productProfile">The profile of the product family, which names the environment variables that override the prerelease flag and the build date at build time.</param>
    protected ComponentInfoBase( Assembly metadataAssembly, ProductProfile productProfile ) : this(
        metadataAssembly,
        productProfile.EnvironmentVariablePrefix ) { }

    private ComponentInfoBase( Assembly metadataAssembly, string environmentVariablePrefix )
    {
        var reader = AssemblyMetadataReader.GetInstance( metadataAssembly );
        this.PackageVersion = reader.PackageVersion;
        this.AssemblyVersion = reader.AssemblyVersion;

        // IsPrerelease flag can be overridden for testing purposes.
        var isPrereleaseEnvironmentVariableValue = Environment.GetEnvironmentVariable( environmentVariablePrefix + "IS_PRERELEASE" );
        bool? isPrereleaseOverriddenValue = isPrereleaseEnvironmentVariableValue == null ? null : bool.Parse( isPrereleaseEnvironmentVariableValue );
        this.IsPrerelease = isPrereleaseOverriddenValue ?? (this.PackageVersion != null && VersionHelper.IsPrereleaseVersion( this.PackageVersion ));

        // BuildDate value can be overridden for testing purposes.
        var buildDateEnvironmentVariableValue = Environment.GetEnvironmentVariable( environmentVariablePrefix + "BUILD_DATE" );

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