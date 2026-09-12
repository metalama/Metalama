// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System.Collections.Immutable;

namespace Metalama.Backstage.Application;

/// <summary>
/// Describes the product family that hosts the Backstage services: its name, its vendor, and the names it uses on the
/// machine for directories, environment variables, locks and log files.
/// </summary>
/// <remarks>
/// <para>
/// The profile is a different axis than <see cref="IApplicationInfo"/>. The application info describes the host
/// process (the compiler, the design-time process, a command-line tool), and one product family has many host
/// processes. The profile describes the product family, and it is the same for all of its host processes. The host
/// passes the profile in <see cref="CoreInitializationOptions.ProductProfile"/>, and the services resolve it
/// through the service provider.
/// </para>
/// <para>
/// The profile is registered as a service so that any component can resolve it. It is immutable.
/// </para>
/// </remarks>
/// <param name="Name">The display name of the product family, for instance <c>Metalama</c>. It appears in messages and in notifications.</param>
/// <param name="Company">The name of the vendor as it appears in the <c>Company</c> attribute of the assemblies of the product. Licensing uses it to identify the components that the vendor built.</param>
/// <param name="DataDirectoryName">The name of the directory, under the local application data directory of the user, that holds the configuration and the temporary files of the product.</param>
/// <param name="EnvironmentVariablePrefix">The prefix of the environment variables that configure the product, including the trailing separator, for instance <c>METALAMA_</c>.</param>
/// <param name="GlobalLockNamePrefix">The prefix of the names of the machine-wide locks of the product, for instance <c>Global\Metalama_</c>.</param>
/// <param name="LicensePropertyName">The name of the MSBuild property, and of the environment variable, that supplies a license key to a build, for instance <c>MetalamaLicense</c>.</param>
/// <param name="AssemblyNamePrefix">The prefix of the names of the assemblies of the product. It identifies the processes that have loaded the product.</param>
[PublicAPI]
public sealed record ProductProfile(
    string Name,
    string Company,
    string DataDirectoryName,
    string EnvironmentVariablePrefix,
    string GlobalLockNamePrefix,
    string LicensePropertyName,
    string AssemblyNamePrefix ) : IBackstageService
{
    /// <summary>
    /// Gets the name of the directory that an earlier version of the product used under the roaming application data
    /// directory, or under the user profile directory. The directory is used when it exists, so that the settings of
    /// an existing installation are not lost. The value is <c>null</c> when the product has no such directory.
    /// </summary>
    public string? LegacyDataDirectoryName { get; init; }

    /// <summary>
    /// Gets the name of the configuration file that a repository commits at its root to configure the product, for
    /// instance <c>metalama.json</c>, or <c>null</c> when the product has no repository configuration file.
    /// </summary>
    public string? RepositoryConfigurationFileName { get; init; }

    /// <summary>
    /// Gets the prefixes of the names of the assemblies and namespaces that an exception report may disclose because
    /// they belong to the vendor. The names of other assemblies and namespaces are redacted from the reports. The
    /// framework assemblies are always disclosed and do not need to be listed.
    /// </summary>
    public ImmutableArray<string> TrustedAssemblyNamePrefixes { get; init; } = ImmutableArray<string>.Empty;

    /// <summary>
    /// Gets the name of an environment variable of the product from its suffix, by prepending
    /// <see cref="EnvironmentVariablePrefix"/>.
    /// </summary>
    /// <param name="suffix">The suffix of the variable, for instance <c>TEMP</c>.</param>
    /// <returns>The full name of the variable, for instance <c>METALAMA_TEMP</c>.</returns>
    public string GetEnvironmentVariableName( string suffix ) => this.EnvironmentVariablePrefix + suffix;
}
