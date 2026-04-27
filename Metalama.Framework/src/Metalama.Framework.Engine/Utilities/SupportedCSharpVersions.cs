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
#if ROSLYN_5_0_0_OR_GREATER
        => LanguageVersion.CSharp14;
#else
        => LanguageVersion.CSharp13;
#endif

#pragma warning disable SA1114 // Parameter list should follow declaration
    /// <summary>
    /// Gets all supported language versions.
    /// </summary>
    public static ImmutableHashSet<LanguageVersion> All { get; } = ImmutableHashSet.Create(
#if ROSLYN_5_0_0_OR_GREATER
        LanguageVersion.CSharp14,
#endif
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
            _ => throw new AssertionFailedException( $"Unexpected Roslyn API version {apiVersion}." )
        };

    internal static string ToNuGetVersionString( this RoslynApiVersion roslynVersion )
        => roslynVersion switch
        {
            RoslynApiVersion.V4_0_1 => "4.0.1",
            RoslynApiVersion.V4_4_0 => "4.4.0",
            RoslynApiVersion.V4_8_0 => "4.8.0",
            RoslynApiVersion.V4_12_0 => "4.12.0",
            RoslynApiVersion.V5_0_0 => "5.0.0",
            _ => throw new AssertionFailedException( $"Unexpected Roslyn version {roslynVersion}." )
        };

    internal static Version ToVersion( this RoslynApiVersion roslynApiVersion )
        => roslynApiVersion switch
        {
            RoslynApiVersion.V4_0_1 => new Version( 4, 0, 1 ),
            RoslynApiVersion.V4_4_0 => new Version( 4, 4, 0 ),
            RoslynApiVersion.V4_8_0 => new Version( 4, 8, 0 ),
            RoslynApiVersion.V4_12_0 => new Version( 4, 12, 0 ),
            RoslynApiVersion.V5_0_0 => new Version( 5, 0, 0 ),
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