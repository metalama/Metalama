// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Metalama.DesignTime.Hosting;

/// <summary>
/// Returns the paths of the analyzer assemblies that a project referencing the <c>Metalama.Framework</c> package gets, from
/// the layout of a NuGet global packages folder.
/// </summary>
/// <remarks>
/// <para>
/// A project that references <c>Metalama.Framework</c> gets the analyzers of two packages: the analyzers of
/// <c>Metalama.Framework</c> itself, which include <c>Metalama.Framework.CompilerExtensions.dll</c>, and
/// <c>Metalama.Compiler.Interface.dll</c>, from the <c>Metalama.Compiler</c> package. The source generator of the first one
/// depends on the second one, so a host that omits it cannot create the generator.
/// </para>
/// <para>
/// The version of <c>Metalama.Compiler</c> is read from the dependencies of the <c>.nuspec</c> file of
/// <c>Metalama.Framework</c>.
/// </para>
/// </remarks>
public static class MetalamaAnalyzerPaths
{
    private const string _analyzerDirectory = "analyzers/dotnet/cs";

    /// <summary>
    /// Returns the analyzer paths for the <c>Metalama.Framework</c> package of a given version in a global packages folder.
    /// </summary>
    /// <param name="packagesFolder">The global packages folder, for instance <c>%USERPROFILE%\.nuget\packages</c>.</param>
    /// <param name="metalamaVersion">The version of the <c>Metalama.Framework</c> package.</param>
    public static ImmutableArray<string> FromPackagesFolder( string packagesFolder, string metalamaVersion )
        => FromPackageDirectory( Path.Combine( packagesFolder, "metalama.framework", metalamaVersion.ToLowerInvariant() ) );

    /// <summary>
    /// Returns the analyzer paths for the directory of a <c>Metalama.Framework</c> package in a global packages folder, for
    /// instance <c>%USERPROFILE%\.nuget\packages\metalama.framework\2027.0.5</c>.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">The package directory, or the directory of the <c>Metalama.Compiler</c>
    /// package that it depends on, does not exist.</exception>
    public static ImmutableArray<string> FromPackageDirectory( string metalamaFrameworkPackageDirectory )
    {
        var frameworkDirectory = Path.GetFullPath( metalamaFrameworkPackageDirectory );

        if ( !Directory.Exists( frameworkDirectory ) )
        {
            throw new DirectoryNotFoundException( $"The directory '{frameworkDirectory}' does not exist." );
        }

        var nuspecPath = Directory.GetFiles( frameworkDirectory, "*.nuspec" ).SingleOrDefault()
                         ?? throw new FileNotFoundException( $"The directory '{frameworkDirectory}' contains no .nuspec file." );

        var compilerVersion = GetDependencyVersion( nuspecPath, "Metalama.Compiler" )
                              ?? throw new InvalidOperationException( $"The file '{nuspecPath}' does not declare a dependency on Metalama.Compiler." );

        // The layout of a global packages folder is <id>/<version>, both in lower case.
        var packagesFolder = Path.GetDirectoryName( Path.GetDirectoryName( frameworkDirectory ) )!;
        var compilerDirectory = Path.Combine( packagesFolder, "metalama.compiler", compilerVersion.ToLowerInvariant() );

        if ( !Directory.Exists( compilerDirectory ) )
        {
            throw new DirectoryNotFoundException( $"The directory '{compilerDirectory}' of the Metalama.Compiler package does not exist. Restore the project first." );
        }

        return GetAnalyzerAssemblies( frameworkDirectory ).AddRange( GetAnalyzerAssemblies( compilerDirectory ) );
    }

    private static ImmutableArray<string> GetAnalyzerAssemblies( string packageDirectory )
    {
        var directory = Path.Combine( packageDirectory, _analyzerDirectory );

        return Directory.Exists( directory )
            ? Directory.GetFiles( directory, "*.dll" ).OrderBy( p => p, StringComparer.OrdinalIgnoreCase ).ToImmutableArray()
            : ImmutableArray<string>.Empty;
    }

    /// <summary>
    /// Returns the lowest version that a <c>.nuspec</c> file accepts for a dependency, or <c>null</c> when it does not declare
    /// the dependency.
    /// </summary>
    /// <remarks>
    /// A version can be a plain version, which is a minimum, or an interval such as <c>[1.0]</c> or <c>[1.0, 2.0)</c>. In both
    /// cases, the lowest version is the one that NuGet restores when no other constraint applies.
    /// </remarks>
    private static string? GetDependencyVersion( string nuspecPath, string packageId )
    {
        var document = XDocument.Load( nuspecPath );

        var version = document.Descendants()
            .Where( e => e.Name.LocalName == "dependency" )
            .FirstOrDefault( e => string.Equals( (string?) e.Attribute( "id" ), packageId, StringComparison.OrdinalIgnoreCase ) )
            ?.Attribute( "version" )
            ?.Value;

        return version?.Trim( '[', ']', '(', ')', ' ' ).Split( ',' )[0].Trim();
    }
}
