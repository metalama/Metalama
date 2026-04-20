// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class ProjectFeaturesTests
{
    // --- Modern .NET (5+) — supported ---

    [Theory]
    [InlineData( "net5.0" )]
    [InlineData( "net6.0" )]
    [InlineData( "net7.0" )]
    [InlineData( "net8.0" )]
    [InlineData( "net9.0" )]
    [InlineData( "net10.0" )]
    public void ModernNet_SupportsCovariantReturn( string tfm )
    {
        Assert.True( ProjectModel.ProjectFeaturesImpl.TargetFrameworkSupportsCovariantReturn( tfm ) );
    }

    // --- Modern .NET with platform suffix — supported ---

    [Theory]
    [InlineData( "net8.0-windows" )]
    [InlineData( "net6.0-android" )]
    [InlineData( "net7.0-ios" )]
    [InlineData( "net8.0-maccatalyst" )]
    public void ModernNetWithPlatform_SupportsCovariantReturn( string tfm )
    {
        Assert.True( ProjectModel.ProjectFeaturesImpl.TargetFrameworkSupportsCovariantReturn( tfm ) );
    }

    // --- Old .NET Framework (no dot) — not supported ---

    [Theory]
    [InlineData( "net472" )]
    [InlineData( "net48" )]
    [InlineData( "net461" )]
    [InlineData( "net452" )]
    [InlineData( "net35" )]
    [InlineData( "net20" )]
    public void NetFrameworkNoDot_DoesNotSupportCovariantReturn( string tfm )
    {
        Assert.False( ProjectModel.ProjectFeaturesImpl.TargetFrameworkSupportsCovariantReturn( tfm ) );
    }

    // --- .NET Framework new-style TFMs (net4.x) — not supported ---

    [Theory]
    [InlineData( "net4.8" )]
    [InlineData( "net4.7.2" )]
    [InlineData( "net4.6.1" )]
    public void NetFrameworkNewStyle_DoesNotSupportCovariantReturn( string tfm )
    {
        Assert.False( ProjectModel.ProjectFeaturesImpl.TargetFrameworkSupportsCovariantReturn( tfm ) );
    }

    // --- .NET Standard — not supported ---

    [Theory]
    [InlineData( "netstandard2.0" )]
    [InlineData( "netstandard2.1" )]
    [InlineData( "netstandard1.6" )]
    public void NetStandard_DoesNotSupportCovariantReturn( string tfm )
    {
        Assert.False( ProjectModel.ProjectFeaturesImpl.TargetFrameworkSupportsCovariantReturn( tfm ) );
    }

    // --- .NET Core — not supported ---

    [Theory]
    [InlineData( "netcoreapp3.1" )]
    [InlineData( "netcoreapp2.1" )]
    [InlineData( "netcoreapp1.0" )]
    public void NetCore_DoesNotSupportCovariantReturn( string tfm )
    {
        Assert.False( ProjectModel.ProjectFeaturesImpl.TargetFrameworkSupportsCovariantReturn( tfm ) );
    }

    // --- Other / edge cases — not supported ---

    [Theory]
    [InlineData( "monoandroid" )]
    [InlineData( "xamarin.ios" )]
    [InlineData( "" )]
    [InlineData( "net" )]
    [InlineData( "abc" )]
    public void OtherTfms_DoesNotSupportCovariantReturn( string tfm )
    {
        Assert.False( ProjectModel.ProjectFeaturesImpl.TargetFrameworkSupportsCovariantReturn( tfm ) );
    }
}