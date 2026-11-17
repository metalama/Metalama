// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;
using Xunit;

namespace Metalama.Mcp.Tests;

public sealed class CommandLineOptionsTests
{
    [Fact]
    public void OutputDirectoryIsLeftToTheWorkerByDefault()
    {
        // The worker then takes a temporary directory of Metalama; see ServerEndToEndTests.HtmlGoesToTheTemporaryDirectoryOfMetalama.
        Assert.Null( CommandLineOptions.Parse( [] ).OutputDirectory );
    }

    [Fact]
    public void OutputDirectoryOptionIsMadeAbsolute()
    {
        var options = CommandLineOptions.Parse( ["--output-dir", "results"] );

        Assert.Equal( Path.GetFullPath( "results" ), options.OutputDirectory );
    }

    [Fact]
    public void EditorLinkDefaultsToVisualStudioCode()
    {
        Assert.Equal( "vscode://file{path}:{line}", CommandLineOptions.Parse( [] ).EditorLink );
    }

    [Fact]
    public void EditorLinkCanBeChangedOrDisabled()
    {
        Assert.Equal( "jetbrains://rider/navigate/reference?path={path}:{line}", CommandLineOptions.Parse( ["--editor-link", "jetbrains://rider/navigate/reference?path={path}:{line}"] ).EditorLink );
        Assert.Null( CommandLineOptions.Parse( ["--editor-link", "none"] ).EditorLink );
    }

    [Fact]
    public void NetworkOptionsAreRejected()
    {
        var options = CommandLineOptions.Parse( ["--port", "8080"] );

        Assert.NotNull( options.Error );
    }
}
