// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Mcp.Tests;

/// <summary>
/// After the worker stops, the next call starts a new worker and reloads the workspaces of the previous one.
/// </summary>
public sealed class WorkerRestartTests
{
    [Fact]
    public async Task WorkspaceThatCannotBeReloadedIsReported()
    {
        var directory = Path.Combine( Path.GetTempPath(), "metalama-mcp-tests", Guid.NewGuid().ToString( "N" ) );
        Directory.CreateDirectory( directory );
        var projectPath = Path.Combine( directory, "Tiny.csproj" );

        await File.WriteAllTextAsync(
            projectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                </PropertyGroup>
            </Project>
            """,
            TestContext.Current.CancellationToken );

        await File.WriteAllTextAsync( Path.Combine( directory, "Code.cs" ), "class Tiny {}", TestContext.Current.CancellationToken );

        try
        {
            await using var server = await ServerProcess.StartAsync();
            await server.CallSuccessfullyAsync( "load_workspace", new Dictionary<string, object?> { ["paths"] = new[] { projectPath } } );

            // The project disappears, then the worker stops.
            File.Delete( projectPath );
            var (stopped, _, _) = await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "Environment.Exit( 3 );" } );
            Assert.True( stopped );

            var text = await server.QueryAsync( "1 + 1" );

            Assert.Contains( "could not be reloaded", text, StringComparison.Ordinal );
            Assert.Contains( "Tiny.csproj", text, StringComparison.Ordinal );
            Assert.DoesNotContain( "reloaded .", text, StringComparison.Ordinal );
        }
        finally
        {
            Directory.Delete( directory, true );
        }
    }
}
