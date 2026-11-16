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
    public void DefaultOutputDirectoryBelongsToTheUser()
    {
        var options = CommandLineOptions.Parse( [] );

        // A fixed name under the temporary directory is shared by all users on Linux (/tmp).
        Assert.False(
            options.OutputDirectory.StartsWith( Path.GetTempPath(), StringComparison.OrdinalIgnoreCase ),
            options.OutputDirectory );

        Assert.StartsWith(
            Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
            options.OutputDirectory,
            StringComparison.OrdinalIgnoreCase );
    }

    [Fact]
    public void NetworkOptionsAreRejected()
    {
        var options = CommandLineOptions.Parse( ["--port", "8080"] );

        Assert.NotNull( options.Error );
    }
}
