// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests <see cref="LanguageVersionExtensions.ToDisplayStringSafe"/>, which formats a language version for a
/// diagnostic message. The method must never throw, because it is called while the arguments of
/// <c>LAMA0051</c> and <c>LAMA0052</c> are built, and an exception there replaces the intended diagnostic by
/// <c>LAMA0001</c>. See issue #1928. The tests of
/// <see cref="LanguageVersionExtensions.OrPreviewIfNotSupported"/> are here as well, because that method is declared
/// by the same class. See issue #1935.
/// </summary>
public sealed class LanguageVersionExtensionsTests
{
    /// <summary>
    /// Verifies that a numeric language version is formatted the way the compiler formats it, including a version
    /// that the running Roslyn does not know. The values 1500 and 1600 are not members of
    /// <see cref="LanguageVersion"/> in the Roslyn versions that Metalama consumes today, and a manifest or a
    /// project option can carry them.
    /// </summary>
    [Theory]
    [InlineData( 703, "7.3" )]
    [InlineData( 800, "8.0" )]
    [InlineData( 1300, "13.0" )]
    [InlineData( 1400, "14.0" )]
    [InlineData( 1500, "15.0" )]
    [InlineData( 1600, "16.0" )]
    public void NumericVersionIsFormatted( int version, string expected )
        => Assert.Equal( expected, ((LanguageVersion) version).ToDisplayStringSafe() );

    /// <summary>
    /// Verifies that the known members of <see cref="LanguageVersion"/> are formatted exactly as the compiler
    /// formats them, so that adding the fallback for an unknown value does not change the text of the existing
    /// diagnostics.
    /// </summary>
    [Theory]
    [InlineData( LanguageVersion.CSharp10 )]
    [InlineData( LanguageVersion.CSharp11 )]
    [InlineData( LanguageVersion.CSharp12 )]
    [InlineData( LanguageVersion.CSharp13 )]
    public void KnownVersionMatchesTheCompilerDisplayString( LanguageVersion version )
        => Assert.Equal( version.ToDisplayString(), version.ToDisplayStringSafe() );

    /// <summary>
    /// Verifies that <see cref="AllLanguageVersions.CSharp15"/> carries the numeric value that the compiler assigns
    /// to C# 15, which is the value that the Roslyn 5.11 window declares. The member is a numeric cast, so nothing
    /// else checks it.
    /// </summary>
    [Fact]
    public void CSharp15HasTheNumericValueOfTheCompiler() => Assert.Equal( 1500, (int) AllLanguageVersions.CSharp15 );

    /// <summary>
    /// Verifies that a version that the running Roslyn declares is returned unchanged.
    /// </summary>
    [Theory]
    [InlineData( LanguageVersion.CSharp10 )]
    [InlineData( LanguageVersion.CSharp11 )]
    [InlineData( LanguageVersion.CSharp12 )]
    [InlineData( LanguageVersion.CSharp13 )]
    [InlineData( LanguageVersion.Preview )]
    public void SupportedVersionIsReturnedUnchanged( LanguageVersion version )
        => Assert.Equal( version, version.OrPreviewIfNotSupported() );

    /// <summary>
    /// Verifies that a version that the running Roslyn does not declare yields <see cref="LanguageVersion.Preview"/>,
    /// which is the value under which that Roslyn reaches the features of the version. The value 1600 is not a member
    /// of <see cref="LanguageVersion"/> in any Roslyn that Metalama consumes today, so this arm of the method is
    /// exercised whatever the running Roslyn is.
    /// </summary>
    [Fact]
    public void UnsupportedVersionYieldsPreview()
        => Assert.Equal( LanguageVersion.Preview, ((LanguageVersion) 1600).OrPreviewIfNotSupported() );

    /// <summary>
    /// Verifies that <see cref="AllLanguageVersions.CSharp15"/> yields either itself or
    /// <see cref="LanguageVersion.Preview"/>, whichever the running Roslyn supports. The test states both outcomes
    /// rather than one, because the answer changes when the repository moves to a Roslyn that declares the version,
    /// and the probe exists so that no source has to be edited at that moment.
    /// </summary>
    [Fact]
    public void CSharp15YieldsItselfOrPreview()
    {
        var probedVersion = AllLanguageVersions.CSharp15.OrPreviewIfNotSupported();

        Assert.True(
            probedVersion is AllLanguageVersions.CSharp15 or LanguageVersion.Preview,
            $"The probe returned '{probedVersion.ToDisplayStringSafe()}'." );

        Assert.Equal(
            LanguageVersionFacts.TryParse( "15.0", out var parsedVersion ) && parsedVersion == AllLanguageVersions.CSharp15,
            probedVersion == AllLanguageVersions.CSharp15 );
    }
}
