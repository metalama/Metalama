// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;

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
/// passes the profile in <see cref="BackstageProduct.Profile"/>, and the services resolve it
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
    /// Gets the long display name of the product, for instance <c>Metalama by PostSharp</c>, used where the product
    /// is presented on its own, such as in the title of a window. The default is <see cref="Name"/>.
    /// </summary>
    public string LongName { get; init; } = Name;

    /// <summary>
    /// Gets a value indicating whether the versions of the product that preceded the per-file configuration locks
    /// used a single lock for the whole configuration directory, which the current version can take as well when the
    /// <c>LEGACY_CONFIGURATION_LOCK</c> environment variable of the product is set. The default is <c>false</c>.
    /// </summary>
    public bool HasLegacyConfigurationLock { get; init; }

    /// <summary>
    /// Gets the prefix of the assembly names of the tool applications of the product: the worker is
    /// <c>{prefix}.Worker</c> and the desktop notifier <c>{prefix}.Desktop.Windows</c>. The default is
    /// <see cref="Name"/> followed by <c>.Backstage</c>, for instance <c>Metalama.Backstage</c>.
    /// </summary>
    public string ToolAssemblyNamePrefix { get; init; } = Name + ".Backstage";

    /// <summary>
    /// Gets the name of the command line tool of the product, for instance <c>metalama</c>, or <c>null</c> when the
    /// product has no such tool. The setup pages mention it when it exists.
    /// </summary>
    public string? CommandLineToolName { get; init; }

    /// <summary>
    /// Gets the name of the logo that the tool applications display, which selects one of the logos that they ship,
    /// for instance <c>metalama</c> or <c>postsharp</c>. The default is <see cref="Name"/> in lower case.
    /// </summary>
    public string LogoName { get; init; } = Name.ToLowerInvariant();

    /// <summary>
    /// Gets the full name of an environment variable of the product by prepending <see cref="EnvironmentVariablePrefix"/>
    /// to its name.
    /// </summary>
    /// <param name="name">The name of the variable without the prefix, for instance <c>TEMP</c>.</param>
    /// <returns>The full name of the variable, for instance <c>METALAMA_TEMP</c>.</returns>
    public string GetEnvironmentVariableName( string name ) => this.EnvironmentVariablePrefix + name;
}
