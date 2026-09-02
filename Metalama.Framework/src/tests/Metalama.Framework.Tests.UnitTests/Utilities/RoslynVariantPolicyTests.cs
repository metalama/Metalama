// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.CompilerExtensions;
using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests <see cref="RoslynVariantPolicy"/>, which maps the Roslyn version of the host to the embedded payload
/// variant that serves it.
/// </summary>
public sealed class RoslynVariantPolicyTests
{
    /// <summary>
    /// Verifies that a host at or above the lowest supported Roslyn version, and below the latest variant, is
    /// served by the Roslyn 5.0 variant. That variant covers Rider under PB-2027.0.
    /// </summary>
    [Theory]
    [InlineData( "5.0" )]
    [InlineData( "5.0.0" )]
    [InlineData( "5.3.0" )]
    [InlineData( "5.9.9" )]
    public void SupportedVersionBelowLatestSelectsThe500Variant( string version )
    {
        Assert.True( RoslynVariantPolicy.TryGetVariantName( new Version( version ), out var variantName ) );
        Assert.Equal( "5.0.0", variantName );
    }

    /// <summary>
    /// Verifies that a host at or above Roslyn 5.10 is served by the latest variant, including a host whose Roslyn
    /// is newer than any variant we ship.
    /// </summary>
    [Theory]
    [InlineData( "5.10" )]
    [InlineData( "5.10.0" )]
    [InlineData( "5.11.0" )]
    [InlineData( "6.0.0" )]
    public void LatestVersionSelectsThe5100Variant( string version )
    {
        Assert.True( RoslynVariantPolicy.TryGetVariantName( new Version( version ), out var variantName ) );
        Assert.Equal( "5.10.0", variantName );
    }

    /// <summary>
    /// Verifies that no variant serves a host whose Roslyn version is below the lowest supported one. This is the
    /// scenario of issue #1898: version 4.14 is the Roslyn of Visual Studio 2022 17.14, which PB-2027.0 excludes,
    /// and version 4.12 named the variant that issue #1881 removed. Naming a variant that is not embedded makes
    /// the resource lookup of <c>ResourceExtractor.CreateInstance</c> fail, and the failure surfaces from the
    /// constructor of whichever entry point Roslyn instantiated. The expected outcome is no variant, so that every
    /// entry point holds a null implementation and does nothing.
    /// </summary>
    [Theory]
    [InlineData( "4.14.0" )]
    [InlineData( "4.12.0" )]
    [InlineData( "4.8.0" )]
    [InlineData( "3.11.0" )]
    public void UnsupportedVersionSelectsNoVariant( string version )
    {
        var roslynVersion = new Version( version );

        // The test data must stay below the lowest supported version when that version moves.
        Assert.True( roslynVersion < RoslynVariantPolicy.MinimumSupportedRoslynVersion );

        Assert.False( RoslynVariantPolicy.TryGetVariantName( roslynVersion, out _ ) );
    }

    /// <summary>
    /// Verifies that the lowest supported Roslyn version is itself served by a variant, so that the boundary
    /// declared by <see cref="RoslynVariantPolicy.MinimumSupportedRoslynVersion"/> and the boundary implemented by
    /// <see cref="RoslynVariantPolicy.TryGetVariantName"/> cannot drift apart.
    /// </summary>
    [Fact]
    public void MinimumSupportedVersionIsServedByAVariant()
    {
        Assert.True( RoslynVariantPolicy.TryGetVariantName( RoslynVariantPolicy.MinimumSupportedRoslynVersion, out _ ) );
    }
}
