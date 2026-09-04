// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Tests the parsing of the <c>MetalamaCompileTimeTargetFrameworks</c> MSBuild property.
/// </summary>
/// <remarks>
/// The property reaches the compiler through the generated analyzer configuration file, in which a semicolon starts a
/// comment, so the build normalizes it to the comma-separated form that every other list-valued option uses. Both
/// separators must be accepted, because a value set directly through <c>IProjectOptions</c>, as in tests, does not go
/// through that file. See issue #1789.
/// </remarks>
public sealed class CompileTimeTargetFrameworksTests
{
    [Theory]
    [InlineData( "netstandard2.0;net10.0;net48" )]
    [InlineData( "netstandard2.0,net10.0,net48" )]
    [InlineData( "netstandard2.0; net10.0; net48" )]
    [InlineData( " netstandard2.0 , net10.0 , net48 " )]
    public void BothSeparatorsAreAccepted( string value )
    {
        var targetFrameworks = CompileTimeAssemblyLocator.ParseTargetFrameworks( value );

        Assert.Equal( new[] { "netstandard2.0", "net10.0", "net48" }, targetFrameworks.ToArray() );
    }

    [Theory]
    [InlineData( "netstandard2.0" )]
    [InlineData( "netstandard2.0;" )]
    [InlineData( ";netstandard2.0;;" )]
    public void EmptyEntriesAreDropped( string value )
    {
        var targetFrameworks = CompileTimeAssemblyLocator.ParseTargetFrameworks( value );

        Assert.Equal( new[] { "netstandard2.0" }, targetFrameworks.ToArray() );
    }

    /// <summary>
    /// Verifies the condition of issue #1789: before the fix, a semicolon-separated value was truncated to its first
    /// entry, which then failed the requirement of a .NET 6.0 or later target framework and crashed with LAMA0001.
    /// </summary>
    [Fact]
    public void SemicolonSeparatedValueKeepsEveryTargetFramework()
    {
        var targetFrameworks = CompileTimeAssemblyLocator.ParseTargetFrameworks( "netstandard2.0;net10.0;net48" );

        Assert.Contains( "netstandard2.0", targetFrameworks );
        Assert.Contains( "net10.0", targetFrameworks );
        Assert.Contains( "net48", targetFrameworks );
    }
}
