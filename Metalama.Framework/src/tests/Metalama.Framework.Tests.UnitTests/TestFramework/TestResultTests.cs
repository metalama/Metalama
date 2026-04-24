// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Testing.AspectTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

public sealed class TestResultTests
{
    [Fact]
    public void FormatErrorMessageAsComments_MultiLineMessage()
    {
        const string errorMessage = "Program execution failed\nSystem.Exception: Test exception\n   at Program.Main()";

        var comments = TestResult.FormatErrorMessageAsComments( errorMessage ).ToList();

        Assert.Equal( 3, comments.Count );
        Assert.Equal( "// Program execution failed\n", comments[0].ToString() );
        Assert.Equal( "// System.Exception: Test exception\n", comments[1].ToString() );
        Assert.Equal( "//    at Program.Main()\n", comments[2].ToString() );
    }

    [Fact]
    public void FormatErrorMessageAsComments_SingleLineMessage()
    {
        var comments = TestResult.FormatErrorMessageAsComments( "Single line error" ).ToList();

        Assert.Single( comments );
        Assert.Equal( "// Single line error\n", comments[0].ToString() );
    }

    [Fact]
    public void FormatErrorMessageAsComments_NullMessage()
    {
        var comments = TestResult.FormatErrorMessageAsComments( null ).ToList();

        Assert.Empty( comments );
    }

    [Fact]
    public void FormatErrorMessageAsComments_EmptyMessage()
    {
        var comments = TestResult.FormatErrorMessageAsComments( "" ).ToList();

        Assert.Empty( comments );
    }

    [Fact]
    public void FormatErrorMessageAsComments_WindowsLineEndings()
    {
        const string errorMessage = "Line1\r\nLine2\r\nLine3";

        var comments = TestResult.FormatErrorMessageAsComments( errorMessage ).ToList();

        Assert.Equal( 3, comments.Count );
        Assert.Equal( "// Line1\n", comments[0].ToString() );
        Assert.Equal( "// Line2\n", comments[1].ToString() );
        Assert.Equal( "// Line3\n", comments[2].ToString() );
    }
}