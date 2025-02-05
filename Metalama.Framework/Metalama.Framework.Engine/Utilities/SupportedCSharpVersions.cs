// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CompileTime;
using Microsoft.CodeAnalysis.CSharp;
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
    /// Gets the default C# version.
    /// </summary>
    public static LanguageVersion Default
#if ROSLYN_4_12_0_OR_GREATER
        => LanguageVersion.CSharp13;
#elif ROSLYN_4_8_0_OR_GREATER
        => LanguageVersion.CSharp12;
#else
        => LanguageVersion.CSharp11;
#endif

#pragma warning disable SA1114 // Parameter list should follow declaration
    /// <summary>
    /// Gets all supported language versions.
    /// </summary>
    public static ImmutableHashSet<LanguageVersion> All { get; } = ImmutableHashSet.Create(
#if ROSLYN_4_12_0_OR_GREATER
        LanguageVersion.CSharp13,
#endif
#if ROSLYN_4_8_0_OR_GREATER
        LanguageVersion.CSharp12,
#endif
        LanguageVersion.CSharp11,
        LanguageVersion.CSharp10 );

    internal static string[] FormatSupportedVersions() => All.SelectAsArray( x => x.ToDisplayString() );

    /// <summary>
    /// Gets the default parse options.
    /// </summary>
    public static CSharpParseOptions DefaultParseOptions { get; } = CSharpParseOptions.Default.WithLanguageVersion( Default );

    internal static LanguageVersion ToLanguageVersion( this RoslynApiVersion apiVersion )
        => apiVersion switch
        {
            RoslynApiVersion.V4_0_1 => (LanguageVersion) 1000,
            RoslynApiVersion.V4_4_0 => (LanguageVersion) 1100,
            RoslynApiVersion.V4_8_0 => (LanguageVersion) 1200,
            RoslynApiVersion.V4_12_0 => (LanguageVersion) 1300,
            _ => throw new AssertionFailedException( $"Unexpected Roslyn API version {apiVersion}." )
        };

    internal static string ToNuGetVersionString( this RoslynApiVersion roslynVersion )
        => roslynVersion switch
        {
            RoslynApiVersion.V4_0_1 => "4.0.1",
            RoslynApiVersion.V4_4_0 => "4.4.0",
            RoslynApiVersion.V4_8_0 => "4.8.0",
            RoslynApiVersion.V4_12_0 => "4.12.0-1.final",
            _ => throw new AssertionFailedException( $"Unexpected Roslyn version {roslynVersion}." )
        };
}