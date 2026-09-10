// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests <see cref="LanguageVersionProvider"/>, which gives the compile-time compilation its language version. The
/// provider lowers the version that the project requests to the highest version that the software development kit
/// accepts. See issue #1979.
/// </summary>
public sealed class LanguageVersionProviderTests : UnitTestClass
{
    public LanguageVersionProviderTests( ITestOutputHelper logger ) : base( logger ) { }

    private LanguageVersion GetCompileTimeLanguageVersion( string sdkVersion, LanguageVersion projectLanguageVersion )
    {
        using var testContext = this.CreateTestContext();

        var projectOptions = new ProjectOptionsWithSdkVersion( sdkVersion, projectLanguageVersion );

        var serviceProvider = testContext.ServiceProvider.Global.Underlying.WithProjectScopedServices(
            projectOptions,
            ImmutableArray<PortableExecutableReference>.Empty );

        return new LanguageVersionProvider( serviceProvider ).GetCompileTimeLanguageVersion();
    }

    /// <summary>
    /// Verifies that the preview language version reaches the compile-time compilation unchanged, whichever software
    /// development kit builds the project. The numeric value of <see cref="LanguageVersion.Preview"/> is
    /// <see cref="int.MaxValue"/>, so a comparison with the ceiling of the software development kit lowered it to
    /// that ceiling, and the compile-time code of a project was then parsed at a lower version than its run-time
    /// code. That is the defect of issue #1979.
    /// </summary>
    [Theory]
    [InlineData( "8.0.100" )]
    [InlineData( "9.0.100" )]
    [InlineData( "10.0.100" )]
    [InlineData( "11.0.100-rc.1.26425.128" )]
    public void PreviewVersionIsPreserved( string sdkVersion )
        => Assert.Equal( LanguageVersion.Preview, this.GetCompileTimeLanguageVersion( sdkVersion, LanguageVersion.Preview ) );

    /// <summary>
    /// Verifies that a numbered version above the ceiling of the software development kit is still lowered to that
    /// ceiling, which is what the provider exists for. The preview version is the single exception.
    /// </summary>
    [Theory]
    [InlineData( "8.0.100", AllLanguageVersions.CSharp12 )]
    [InlineData( "9.0.100", AllLanguageVersions.CSharp13 )]
    [InlineData( "10.0.100", AllLanguageVersions.CSharp14 )]
    public void NumberedVersionAboveTheCeilingIsLowered( string sdkVersion, LanguageVersion expected )
        => Assert.Equal( expected, this.GetCompileTimeLanguageVersion( sdkVersion, AllLanguageVersions.CSharp15 ) );

    /// <summary>
    /// Verifies that a numbered version at or below the ceiling of the software development kit reaches the
    /// compile-time compilation unchanged.
    /// </summary>
    [Theory]
    [InlineData( "8.0.100", AllLanguageVersions.CSharp10 )]
    [InlineData( "9.0.100", AllLanguageVersions.CSharp13 )]
    [InlineData( "10.0.100", AllLanguageVersions.CSharp14 )]
    [InlineData( "11.0.100", AllLanguageVersions.CSharp14 )]
    public void NumberedVersionBelowTheCeilingIsPreserved( string sdkVersion, LanguageVersion projectLanguageVersion )
        => Assert.Equal( projectLanguageVersion, this.GetCompileTimeLanguageVersion( sdkVersion, projectLanguageVersion ) );

    /// <summary>
    /// Verifies that the .NET 11 software development kit raises the ceiling to C# 15, so that a request for C# 15 is
    /// no longer lowered to C# 14. The .NET 10 arm of the same request is covered by
    /// <see cref="NumberedVersionAboveTheCeilingIsLowered"/>, and it still yields C# 14.
    /// </summary>
    /// <remarks>
    /// The ceiling of the .NET 11 arm is written as
    /// <see cref="LanguageVersionExtensions.OrPreviewIfNotSupported"/> of <see cref="AllLanguageVersions.CSharp15"/>,
    /// so it is either C# 15 or the preview version, depending on whether the Roslyn that this assembly is bound to
    /// declares C# 15. The result asserted here is the same in both cases, because the numeric value of the preview
    /// version is above every numbered version.
    /// </remarks>
    [Fact]
    public void DotNet11DoesNotLowerCSharp15()
        => Assert.Equal(
            AllLanguageVersions.CSharp15,
            this.GetCompileTimeLanguageVersion( "11.0.100-rc.1.26425.128", AllLanguageVersions.CSharp15 ) );

    /// <summary>
    /// An implementation of <see cref="IProjectOptions"/> that reports a given .NET software development kit version
    /// and a given language version, which are the two inputs of
    /// <see cref="LanguageVersionProvider.GetCompileTimeLanguageVersion"/> on the software development kit path.
    /// </summary>
    private sealed class ProjectOptionsWithSdkVersion : DefaultProjectOptions
    {
        private readonly string _sdkVersion;
        private readonly LanguageVersion _languageVersion;

        public ProjectOptionsWithSdkVersion( string sdkVersion, LanguageVersion languageVersion )
        {
            this._sdkVersion = sdkVersion;
            this._languageVersion = languageVersion;
        }

        public override string? SdkVersion => this._sdkVersion;

        public override LanguageVersion LanguageVersion => this._languageVersion;

        public override bool IsTest => true;
    }
}
