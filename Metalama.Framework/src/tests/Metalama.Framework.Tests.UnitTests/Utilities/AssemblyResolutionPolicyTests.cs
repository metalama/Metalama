// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.CompilerExtensions;
using System.Reflection;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests the rules that <see cref="AssemblyResolutionPolicy"/> applies when an assembly embedded in the current build
/// of Metalama is requested and another build of Metalama is loaded in the same process.
/// </summary>
public sealed class AssemblyResolutionPolicyTests
{
    private const string _backstageName = "Metalama.Backstage";

    private static AssemblyName GetBackstageName( string version )
        => new( $"{_backstageName}, Version={version}, Culture=neutral, PublicKeyToken=d793dee9bee12010" );

    /// <summary>
    /// Verifies that a request for an embedded assembly is not satisfied by a higher version of the same assembly that
    /// another build of Metalama has already loaded in the process. See issue #1833: <c>Metalama.Backstage</c> makes no
    /// API compatibility promise across builds, so binding an older build of Metalama to a newer
    /// <c>Metalama.Backstage</c> throws <see cref="System.TypeLoadException"/> as soon as a removed type is used.
    /// </summary>
    [Fact]
    public void EmbeddedAssemblyDoesNotAcceptHigherVersionOfAlreadyLoadedAssembly()
        => Assert.False( AssemblyResolutionPolicy.AcceptsHigherVersionOfAlreadyLoadedAssembly( _backstageName, isEmbeddedInCurrentBuild: true ) );

    /// <summary>
    /// Verifies that the newer <c>Metalama.Backstage</c> of the reported scenario does not match the request issued by
    /// the older build of Metalama.
    /// </summary>
    [Fact]
    public void NewerBackstageDoesNotMatchRequestOfOlderMetalama()
    {
        var requested = GetBackstageName( "2025.1.17.1043" );
        var alreadyLoaded = GetBackstageName( "2026.1.22.854" );

        Assert.False( AssemblyResolutionPolicy.MatchesExactVersion( requested, alreadyLoaded ) );
        Assert.False( AssemblyResolutionPolicy.AcceptsHigherVersionOfAlreadyLoadedAssembly( requested.Name!, isEmbeddedInCurrentBuild: true ) );
    }

    /// <summary>
    /// Verifies that an embedded assembly that is already loaded with exactly the requested version is still accepted.
    /// </summary>
    [Fact]
    public void EmbeddedAssemblyAcceptsExactVersionOfAlreadyLoadedAssembly()
    {
        var requested = GetBackstageName( "2026.1.22.854" );
        var alreadyLoaded = GetBackstageName( "2026.1.22.854" );

        Assert.True( AssemblyResolutionPolicy.MatchesExactVersion( requested, alreadyLoaded ) );
    }

    /// <summary>
    /// Verifies that a lower version of an embedded assembly never matches the request, whatever the resolution rules.
    /// </summary>
    [Fact]
    public void LowerVersionNeverMatches()
    {
        var requested = GetBackstageName( "2026.1.22.854" );
        var alreadyLoaded = GetBackstageName( "2025.1.17.1043" );

        Assert.False( AssemblyResolutionPolicy.MatchesExactVersion( requested, alreadyLoaded ) );
        Assert.False( AssemblyResolutionPolicy.MatchesSameOrHigherVersion( requested, alreadyLoaded ) );
    }

    /// <summary>
    /// Verifies that an assembly that is not embedded in the current build, typically an assembly provided by the host
    /// process such as Roslyn, is still resolved to a higher version that is already loaded.
    /// </summary>
    [Fact]
    public void NotEmbeddedAssemblyAcceptsHigherVersionOfAlreadyLoadedAssembly()
    {
        Assert.True( AssemblyResolutionPolicy.AcceptsHigherVersionOfAlreadyLoadedAssembly( "Microsoft.CodeAnalysis", isEmbeddedInCurrentBuild: false ) );

        var requested = new AssemblyName( "Microsoft.CodeAnalysis, Version=4.12.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" );
        var alreadyLoaded = new AssemblyName( "Microsoft.CodeAnalysis, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" );

        Assert.True( AssemblyResolutionPolicy.MatchesSameOrHigherVersion( requested, alreadyLoaded ) );
    }

    /// <summary>
    /// Verifies that a request that does not specify a version is satisfied by any version, which is the case of the
    /// requests issued by <see cref="Assembly.Load(string)"/> with a simple name.
    /// </summary>
    [Fact]
    public void RequestWithoutVersionMatchesAnyVersion()
    {
        var requested = new AssemblyName( _backstageName );
        var alreadyLoaded = GetBackstageName( "2026.1.22.854" );

        Assert.True( AssemblyResolutionPolicy.MatchesSameOrHigherVersion( requested, alreadyLoaded ) );
    }
}
