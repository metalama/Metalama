// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Globalization;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

internal static class LanguageVersionExtensions
{
    public static string ToDisplayStringSafe( this LanguageVersion version )
    {
        // LanguageVersion.ToDisplayString is not safe because it will throw for a version that is not supported for the specific Roslyn version
        // we are bound to.
        return version switch
        {
            LanguageVersion.CSharp1 => "1",
            LanguageVersion.CSharp2 => "2",
            LanguageVersion.CSharp3 => "3",
            LanguageVersion.CSharp4 => "4",
            LanguageVersion.CSharp5 => "5",
            LanguageVersion.CSharp6 => "6",
            LanguageVersion.CSharp7 => "7.0",
            LanguageVersion.CSharp7_1 => "7.1",
            LanguageVersion.CSharp7_2 => "7.2",
            LanguageVersion.CSharp7_3 => "7.3",
            LanguageVersion.CSharp8 => "8.0",
            LanguageVersion.CSharp9 => "9.0",
            LanguageVersion.CSharp10 => "10.0",
            LanguageVersion.CSharp11 => "11.0",
            LanguageVersion.CSharp12 => "12.0",
            (LanguageVersion) 1300 => "13.0",
            (LanguageVersion) 1400 => "14.0",
            (LanguageVersion) 1500 => "15.0",
            LanguageVersion.Default => "default",
            LanguageVersion.Latest => "latest",
            LanguageVersion.LatestMajor => "latestmajor",
            LanguageVersion.Preview => "preview",

            // A version that no arm above matches is formatted from its numeric value, which is how the compiler
            // composes the display string of every version from 7.0 on. Without this fallback the method throws while
            // the arguments of LAMA0051 and LAMA0052 are built, the exception replaces the diagnostic designed for the
            // situation, and the user is asked to open a support ticket for LAMA0001. The threshold is 700 because the
            // versions below it are displayed as a single number, and all of them have an arm. See issue #1928. The
            // discard arm is kept rather than replaced by a relational pattern, because a switch expression over an
            // enum without a discard arm produces an exhaustiveness warning that the build promotes to an error.
            _ => (int) version >= 700
                ? FormatNumericVersion( version )
                : throw new ArgumentOutOfRangeException( nameof(version), $"Invalid language version: '{version}'." )
        };
    }

    /// <summary>
    /// Returns <paramref name="version"/> when the Roslyn that this assembly is bound to recognizes it, and
    /// <see cref="LanguageVersion.Preview"/> otherwise.
    /// </summary>
    /// <remarks>
    /// A member of <see cref="AllLanguageVersions"/> is a numeric cast, so a source file can name a version that the
    /// bound Roslyn does not declare, but a parser of that Roslyn rejects the value. The same features are reached
    /// under <see cref="LanguageVersion.Preview"/> in the Roslyn releases that publish a feature before they name the
    /// version that supports it, so this method returns the fallback in that case. The choice is made from the
    /// running Roslyn and from no build flag, so the method stops returning the fallback by itself once Roslyn
    /// declares the version. See issue #1935.
    /// </remarks>
    public static LanguageVersion OrPreviewIfNotSupported( this LanguageVersion version )
        => LanguageVersionFacts.TryParse( version.ToDisplayStringSafe(), out var parsedVersion ) && parsedVersion == version
            ? version
            : LanguageVersion.Preview;

    private static string FormatNumericVersion( LanguageVersion version )
    {
        var numericVersion = (int) version;

        return (numericVersion / 100).ToString( CultureInfo.InvariantCulture )
               + "."
               + (numericVersion % 100).ToString( CultureInfo.InvariantCulture );
    }
}