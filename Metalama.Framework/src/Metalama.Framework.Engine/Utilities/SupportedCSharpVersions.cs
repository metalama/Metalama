// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.Linq;

// ReSharper disable WrongIndentSize

#pragma warning disable SA1115, SA1113, SA1001, SA1111

namespace Metalama.Framework.Engine.Utilities;

/// <summary>
/// Exposes the versions of the C# language supported by Metalama.
/// </summary>
[PublicAPI]
public static class SupportedCSharpVersions
{
    /// <summary>
    /// Gets the latest C# version supported by the current Metalama build.
    /// </summary>
    /// <remarks>
    /// This C# version might not be supported by the .NET SDK. See also <see cref="LanguageVersionProvider"/>.
    /// </remarks>
    public static LanguageVersion Latest
        => LanguageVersion.CSharp14;

#pragma warning disable SA1114 // Parameter list should follow declaration
    /// <summary>
    /// Gets all supported language versions.
    /// </summary>
    public static ImmutableHashSet<LanguageVersion> All { get; } = ImmutableHashSet.Create(
        LanguageVersion.CSharp14,
        LanguageVersion.CSharp13,
        LanguageVersion.CSharp12,
        LanguageVersion.CSharp11,
        LanguageVersion.CSharp10 );

    internal static string[] FormatSupportedVersions() => All.SelectAsArray( x => x.ToDisplayStringSafe() );

    /// <summary>
    /// Gets the default parse options.
    /// </summary>
    public static CSharpParseOptions DefaultParseOptions { get; } = CSharpParseOptions.Default.WithLanguageVersion( Latest );

    internal static LanguageVersion ToLanguageVersion( this RoslynApiVersion apiVersion )
        => apiVersion switch
        {
            RoslynApiVersion.V4_0_1 => AllLanguageVersions.CSharp10,
            RoslynApiVersion.V4_4_0 => AllLanguageVersions.CSharp11,
            RoslynApiVersion.V4_8_0 => AllLanguageVersions.CSharp12,
            RoslynApiVersion.V4_12_0 => AllLanguageVersions.CSharp13,
            RoslynApiVersion.V5_0_0 => AllLanguageVersions.CSharp14,
            RoslynApiVersion.V5_10_0 => AllLanguageVersions.CSharp14,
            _ => throw new AssertionFailedException( $"Unexpected Roslyn API version {apiVersion}." )
        };

    /// <summary>
    /// Gets the version string under which the packages of a given Roslyn version are published, which is the version
    /// that the reference-assembly project of <see cref="CompileTimeAssemblyLocator"/> requests. The value must be the
    /// exact package version that the variant is built against, that is, <c>RoslynApiMaxVersion</c> of
    /// <c>Directory.Packages.props</c> for the latest variant, so that compile-time code is compiled against the same
    /// API as the one it runs against. A lower version would hide the members that the running Roslyn exposes.
    /// </summary>
    /// <remarks>
    /// This method is the whole of the switch between a released Roslyn and a prerelease one. A prerelease version
    /// string is served by the package source of <see cref="RoslynPrereleaseSourceUrl"/> and not by nuget.org, and
    /// <see cref="ToPrereleasePackageSourceUrl"/> derives that source from the version string returned here, so
    /// entering or leaving a prerelease Roslyn is the edit of the version string alone. See issue #1885.
    /// </remarks>
    internal static string ToNuGetVersionString( this RoslynApiVersion roslynVersion )
        => roslynVersion switch
        {
            RoslynApiVersion.V4_0_1 => "4.0.1",
            RoslynApiVersion.V4_4_0 => "4.4.0",
            RoslynApiVersion.V4_8_0 => "4.8.0",
            RoslynApiVersion.V4_12_0 => "4.12.0",
            RoslynApiVersion.V5_0_0 => "5.0.0",
            RoslynApiVersion.V5_10_0 => "5.10.0-1.26365.3",
            _ => throw new AssertionFailedException( $"Unexpected Roslyn version {roslynVersion}." )
        };

    /// <summary>
    /// The key under which <see cref="RoslynPrereleaseSourceUrl"/> is declared in the <c>nuget.config</c> that
    /// <see cref="CompileTimeAssemblyLocator"/> writes beside the reference-assembly project.
    /// </summary>
    internal const string RoslynPrereleaseSourceKey = "roslyn-consolidated";

    /// <summary>
    /// The address of the package source that consolidates the Roslyn packages of every branch, including the
    /// prerelease ones, which nuget.org does not serve.
    /// </summary>
    internal const string RoslynPrereleaseSourceUrl = "https://proget.postsharp.net/nuget/roslyn-consolidated/v3/index.json";

    /// <summary>
    /// The package source mapping pattern that covers the Roslyn packages requested by the reference-assembly project.
    /// </summary>
    internal const string RoslynPackagePattern = "Microsoft.CodeAnalysis.*";

    /// <summary>
    /// Gets the address of the package source that serves the packages of a given Roslyn version, or <c>null</c> when
    /// nuget.org serves them.
    /// </summary>
    /// <remarks>
    /// The answer is derived from the version string of <see cref="ToNuGetVersionString"/>, so that a version branch
    /// enters or leaves a prerelease Roslyn by editing that version string and nothing else. A method that enumerated
    /// the versions here would have to be edited in step with it, and a version added without that edit would either
    /// request a package that nuget.org does not serve or declare a package source that nothing needs.
    /// See issue #1885.
    /// </remarks>
    internal static string? ToPrereleasePackageSourceUrl( this RoslynApiVersion roslynVersion )
        => GetPrereleasePackageSourceUrl( roslynVersion.ToNuGetVersionString() );

    /// <summary>
    /// Gets the address of the package source that serves the packages published under a given version string, or
    /// <c>null</c> when nuget.org serves them.
    /// </summary>
    /// <remarks>
    /// A version string that carries a prerelease label, that is, a hyphen, belongs to a build that is published on
    /// the feeds consolidated by <see cref="RoslynPrereleaseSourceUrl"/> and not on nuget.org. Declaring the source
    /// for a prerelease that nuget.org happens to serve as well costs one more candidate source and nothing else,
    /// whereas omitting it for a prerelease that nuget.org does not serve fails the restore of the
    /// reference-assembly project on every user machine.
    /// </remarks>
    internal static string? GetPrereleasePackageSourceUrl( string nuGetVersionString )
        => nuGetVersionString.IndexOf( "-", StringComparison.Ordinal ) >= 0 ? RoslynPrereleaseSourceUrl : null;

    internal static Version ToVersion( this RoslynApiVersion roslynApiVersion )
        => roslynApiVersion switch
        {
            RoslynApiVersion.V4_0_1 => new Version( 4, 0, 1 ),
            RoslynApiVersion.V4_4_0 => new Version( 4, 4, 0 ),
            RoslynApiVersion.V4_8_0 => new Version( 4, 8, 0 ),
            RoslynApiVersion.V4_12_0 => new Version( 4, 12, 0 ),
            RoslynApiVersion.V5_0_0 => new Version( 5, 0, 0 ),
            RoslynApiVersion.V5_10_0 => new Version( 5, 10, 0 ),
            _ => throw new AssertionFailedException( $"Unexpected Roslyn version {roslynApiVersion}." )
        };

    /// <summary>
    /// Gets the maximum C# language version supported by a given Roslyn version.
    /// </summary>
    internal static LanguageVersion GetMaxLanguageVersion( Version roslynVersion )
        => (roslynVersion.Major, roslynVersion.Minor) switch
        {
            (>= 5, _) => AllLanguageVersions.CSharp14,
            (4, >= 12) => AllLanguageVersions.CSharp13,
            (4, >= 8) => AllLanguageVersions.CSharp12,
            (4, >= 4) => AllLanguageVersions.CSharp11,
            (4, _) => AllLanguageVersions.CSharp10,
            (3, _) => LanguageVersion.CSharp9,
            _ => throw new PlatformNotSupportedException( $"Unsupported Roslyn version: {roslynVersion}." )
        };
}