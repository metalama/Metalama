// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.CompilerExtensions;
using System.Reflection;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests <see cref="AssemblyResolutionPolicy"/>, which decides which assembly already loaded in the process may satisfy
/// an assembly resolution request of the resource extractor.
/// </summary>
public sealed class AssemblyResolutionPolicyTests
{
    private const string _backstageName = "Metalama.Backstage";

    private static AssemblyName GetBackstageName( string version )
        => new( $"{_backstageName}, Version={version}, Culture=neutral, PublicKeyToken=d793dee9bee12010" );

    private static AssemblyName GetRoslynName( string version )
        => new( $"Microsoft.CodeAnalysis, Version={version}, Culture=neutral, PublicKeyToken=31bf3856ad364e35" );

    /// <summary>
    /// Verifies that a request for an assembly embedded in the current build is not satisfied by a higher version of
    /// the same assembly that another build of Metalama has already loaded in the process. This is the scenario of
    /// issue #1833: an older build of Metalama requested <c>Metalama.Backstage 2025.1.17.1043</c> in a Visual Studio
    /// process where a newer build had already loaded <c>Metalama.Backstage 2026.1.22.854</c>, and the resulting
    /// binding threw <see cref="System.TypeLoadException"/> on the first type that the newer build had removed.
    /// </summary>
    [Fact]
    public void EmbeddedAssemblyIsNotSatisfiedByHigherAlreadyLoadedVersion()
    {
        var requested = GetBackstageName( "2025.1.17.1043" );
        AssemblyName[] alreadyLoaded = [GetBackstageName( "2026.1.22.854" )];

        Assert.Equal( -1, AssemblyResolutionPolicy.SelectAlreadyLoadedAssembly( requested, alreadyLoaded, isEmbeddedInCurrentBuild: true ) );
    }

    /// <summary>
    /// Verifies that an assembly embedded in the current build is still satisfied by an already-loaded assembly of
    /// exactly the requested version, which is what keeps a single copy of the assembly in processes such as
    /// Metalama.Try.
    /// </summary>
    [Fact]
    public void EmbeddedAssemblyIsSatisfiedByExactAlreadyLoadedVersion()
    {
        var requested = GetBackstageName( "2026.1.22.854" );
        AssemblyName[] alreadyLoaded = [GetBackstageName( "2026.1.22.854" )];

        Assert.Equal( 0, AssemblyResolutionPolicy.SelectAlreadyLoadedAssembly( requested, alreadyLoaded, isEmbeddedInCurrentBuild: true ) );
    }

    /// <summary>
    /// Verifies that an assembly of a lower version is never selected.
    /// </summary>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void LowerAlreadyLoadedVersionIsNeverSelected( bool isEmbeddedInCurrentBuild )
    {
        var requested = GetBackstageName( "2026.1.22.854" );
        AssemblyName[] alreadyLoaded = [GetBackstageName( "2025.1.17.1043" )];

        Assert.Equal( -1, AssemblyResolutionPolicy.SelectAlreadyLoadedAssembly( requested, alreadyLoaded, isEmbeddedInCurrentBuild ) );
    }

    /// <summary>
    /// Verifies that an assembly that is not embedded in the current build, typically an assembly provided by the host
    /// process such as Roslyn, is still satisfied by a higher version that is already loaded. Our own assemblies may
    /// request a lower version of Roslyn than the one that the host has loaded.
    /// </summary>
    [Fact]
    public void NotEmbeddedAssemblyIsSatisfiedByHigherAlreadyLoadedVersion()
    {
        var requested = GetRoslynName( "4.12.0.0" );
        AssemblyName[] alreadyLoaded = [GetRoslynName( "5.0.0.0" )];

        Assert.Equal( 0, AssemblyResolutionPolicy.SelectAlreadyLoadedAssembly( requested, alreadyLoaded, isEmbeddedInCurrentBuild: false ) );
    }

    /// <summary>
    /// Verifies that the exact requested version is preferred over a higher one, whatever the order in which the
    /// assemblies were loaded.
    /// </summary>
    [Fact]
    public void ExactAlreadyLoadedVersionIsPreferredOverHigherOne()
    {
        var requested = GetRoslynName( "4.12.0.0" );
        AssemblyName[] alreadyLoaded = [GetRoslynName( "5.0.0.0" ), GetRoslynName( "4.12.0.0" )];

        Assert.Equal( 1, AssemblyResolutionPolicy.SelectAlreadyLoadedAssembly( requested, alreadyLoaded, isEmbeddedInCurrentBuild: false ) );
    }

    /// <summary>
    /// Verifies that a request that does not specify a version, as issued by <see cref="Assembly.Load(string)"/> with a
    /// simple name, is satisfied by any version of an assembly that is not embedded in the current build.
    /// </summary>
    [Fact]
    public void RequestWithoutVersionIsSatisfiedByAnyVersion()
    {
        var requested = new AssemblyName( "Microsoft.CodeAnalysis" );
        AssemblyName[] alreadyLoaded = [GetRoslynName( "5.0.0.0" )];

        Assert.Equal( 0, AssemblyResolutionPolicy.SelectAlreadyLoadedAssembly( requested, alreadyLoaded, isEmbeddedInCurrentBuild: false ) );
    }

    /// <summary>
    /// Verifies that no assembly is selected when none of the already-loaded assemblies has the requested name.
    /// </summary>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void NoAssemblyIsSelectedWhenNoneMatchesTheName( bool isEmbeddedInCurrentBuild )
    {
        var requested = GetBackstageName( "2026.1.22.854" );
        AssemblyName[] alreadyLoaded = [GetRoslynName( "5.0.0.0" )];

        Assert.Equal( -1, AssemblyResolutionPolicy.SelectAlreadyLoadedAssembly( requested, alreadyLoaded, isEmbeddedInCurrentBuild ) );
    }
}
