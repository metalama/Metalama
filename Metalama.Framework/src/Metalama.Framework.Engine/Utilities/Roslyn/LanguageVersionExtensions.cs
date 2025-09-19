// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp;
using System;

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
            LanguageVersion.Default => "default",
            LanguageVersion.Latest => "latest",
            LanguageVersion.LatestMajor => "latestmajor",
            LanguageVersion.Preview => "preview",
            _ => throw new ArgumentOutOfRangeException( nameof(version), $"Invalid language version: '{version}'." )
        };
    }
}