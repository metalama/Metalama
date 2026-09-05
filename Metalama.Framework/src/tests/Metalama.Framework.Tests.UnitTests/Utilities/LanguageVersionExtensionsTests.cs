// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests <see cref="LanguageVersionExtensions.ToDisplayStringSafe"/>, which formats a language version for a
/// diagnostic message. The method must never throw, because it is called while the arguments of
/// <c>LAMA0051</c> and <c>LAMA0052</c> are built, and an exception there replaces the intended diagnostic by
/// <c>LAMA0001</c>. See issue #1928.
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
}
