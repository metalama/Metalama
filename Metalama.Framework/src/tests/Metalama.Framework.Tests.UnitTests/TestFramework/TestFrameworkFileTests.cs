// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Testing.AspectTesting;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

/// <summary>
/// Tests of <see cref="TestFrameworkFile"/>, which decides whether the diagnostics and the end of lines of a file
/// belong to the test or to the framework.
/// </summary>
/// <remarks>
/// <para>
/// The aspect test suites cannot cover this, because a file whose name begins with an underscore is not discovered
/// as a test, and the diagnostics of an included file are attached to that file rather than to the test that
/// includes it. A predicate that accepted one or two underscores would therefore silently drop the warnings of a
/// source file of a test, and every suite would stay green.
/// </para>
/// </remarks>
public sealed class TestFrameworkFileTests
{
    [Theory]
    [InlineData( "___Polyfill_IsExternalInit.cs" )]
    [InlineData( "___GlobalUsings.cs" )]
    [InlineData( @"C:\Whatever\___Polyfill_IUnion.cs" )]
    public void FileAddedByTheFrameworkIsRecognized( string path )
    {
        Assert.True( TestFrameworkFile.IsTestFrameworkFile( path ) );
    }

    [Theory]
    [InlineData( "_Common.cs" )]
    [InlineData( "__TopLevelStatements.cs" )]
    [InlineData( "MyTest.cs" )]
    [InlineData( null )]
    public void FileOfTheTestIsNotRecognized( string? path )
    {
        Assert.False( TestFrameworkFile.IsTestFrameworkFile( path ) );
    }
}
