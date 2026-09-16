// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests <see cref="SupportedCSharpVersions.GetMaxLanguageVersion"/>, which answers the highest C# version that a
/// given Roslyn accepts.
/// </summary>
/// <remarks>
/// The method is the msbuild.exe path of <see cref="LanguageVersionProvider"/>, which reads the version of the
/// Roslyn bundled with the Visual Studio installation instead of the version of the .NET software development kit.
/// <see cref="LanguageVersionProviderTests"/> covers the software development kit path, and the two paths share no
/// code. See issue #1937.
/// </remarks>
public sealed class SupportedCSharpVersionsTests : UnitTestClass
{
    public SupportedCSharpVersionsTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Verifies the version that each Roslyn is mapped to, including a major version above the highest one that the
    /// method lists.
    /// </summary>
    /// <remarks>
    /// The arm of C# 15 required the major version to be at least 5 and the minor version to be at least 11. A minor
    /// version restarts at zero in a new major version, so a Roslyn 6.0 did not match that arm, fell through to the
    /// arm below it and was reported as accepting C# 14 only. The two cases above major version 5 are what keeps
    /// that defect from returning.
    /// </remarks>
    [Theory]
    [InlineData( 3, 0, LanguageVersion.CSharp9 )]
    [InlineData( 4, 0, AllLanguageVersions.CSharp10 )]
    [InlineData( 4, 4, AllLanguageVersions.CSharp11 )]
    [InlineData( 4, 8, AllLanguageVersions.CSharp12 )]
    [InlineData( 4, 12, AllLanguageVersions.CSharp13 )]
    [InlineData( 5, 0, AllLanguageVersions.CSharp14 )]
    [InlineData( 5, 10, AllLanguageVersions.CSharp14 )]
    [InlineData( 5, 11, AllLanguageVersions.CSharp15 )]
    [InlineData( 5, 12, AllLanguageVersions.CSharp15 )]
    [InlineData( 6, 0, AllLanguageVersions.CSharp15 )]
    [InlineData( 7, 3, AllLanguageVersions.CSharp15 )]
    public void RoslynVersionIsMappedToLanguageVersion( int major, int minor, LanguageVersion expected )
        => Assert.Equal( expected, SupportedCSharpVersions.GetMaxLanguageVersion( new Version( major, minor ) ) );

    /// <summary>
    /// Verifies that a Roslyn below the oldest one that the method lists is refused rather than mapped to a version
    /// that it does not accept.
    /// </summary>
    [Fact]
    public void RoslynVersionBelowTheOldestIsRefused()
        => Assert.Throws<PlatformNotSupportedException>( () => SupportedCSharpVersions.GetMaxLanguageVersion( new Version( 2, 0 ) ) );
}
