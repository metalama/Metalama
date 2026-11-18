// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Mcp.Tests;

/// <summary>
/// The supervisor must survive anything the worker does, because the worker runs arbitrary code, and the worker must not
/// outlive the supervisor.
/// </summary>
public sealed class WorkerIsolationTests
{
    /// <summary>
    /// A statement that writes raw bytes to the pipe from the worker to the supervisor, as code running in the worker can,
    /// because the handle values are on the command line of the worker.
    /// </summary>
    private const string _writeToSupervisorPipe = """
                                                  var commandLine = Environment.GetCommandLineArgs();
                                                  var handle = commandLine[Array.IndexOf( commandLine, "--worker" ) + 2];
                                                  var pipe = new System.IO.FileStream(
                                                      new Microsoft.Win32.SafeHandles.SafeFileHandle( (IntPtr) long.Parse( handle ), false ),
                                                      System.IO.FileAccess.Write );
                                                  pipe.Write( bytes );
                                                  pipe.Flush();
                                                  """;

    [Fact]
    public async Task ResponseWithAnotherIdIsRejected()
    {
        await using var server = await ServerProcess.StartAsync();

        var code = """
                   var payload = Encoding.UTF8.GetBytes( "{\"id\":999999,\"ok\":true,\"text\":\"forged\"}" );
                   var bytes = new byte[4 + payload.Length];
                   BitConverter.GetBytes( payload.Length ).CopyTo( bytes, 0 );
                   payload.CopyTo( bytes, 4 );
                   """ + _writeToSupervisorPipe + "\nreturn \"genuine\";";

        var (isError, text, _) = await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = code } );

        Assert.DoesNotContain( "forged", text, StringComparison.Ordinal );
        Assert.True( isError, text );

        // The next call gets its own response, not one left in the pipe.
        Assert.StartsWith( "2\n", await server.QueryAsync( "1 + 1" ), StringComparison.Ordinal );
    }

    [Fact]
    public async Task MalformedFrameIsAWorkerFailure()
    {
        await using var server = await ServerProcess.StartAsync();

        var code = "var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0x7F, 0x00 };\n" + _writeToSupervisorPipe + "\nreturn 0;";

        var (isError, text, _) = await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = code } );

        Assert.True( isError, text );
        Assert.Contains( "worker", text, StringComparison.OrdinalIgnoreCase );

        Assert.StartsWith( "2\n", await server.QueryAsync( "1 + 1" ), StringComparison.Ordinal );
    }

    [Fact]
    public async Task WorkerExitsWhenTheSupervisorDiesDuringARequest()
    {
        var server = await ServerProcess.StartAsync();
        await server.QueryAsync( "0" );
        var (supervisorId, workerId) = await server.WaitForProcessIdsAsync();

        // A query that never ends, then the supervisor is terminated without a chance to clean up.
        _ = server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "while ( true ) { }", ["timeoutSeconds"] = 600 } );
        await Task.Delay( 2000, TestContext.Current.CancellationToken );
        Process.GetProcessById( supervisorId ).Kill( entireProcessTree: false );

        var worker = Process.GetProcessById( workerId );

        try
        {
            Assert.True( worker.WaitForExit( 15_000 ), "The worker kept running after the supervisor died." );
        }
        finally
        {
            if ( !worker.HasExited )
            {
                worker.Kill( entireProcessTree: true );
            }

            try
            {
                await server.DisposeAsync();
            }
            catch ( Exception e ) when ( e is not OutOfMemoryException )
            {
                // The supervisor was killed.
            }
        }
    }

    [Fact]
    public async Task WorkerCrashIsDetectedEvenWhenAChildProcessOutlivesIt()
    {
        await using var server = await ServerProcess.StartAsync();

        // A child that inherits the handles of the worker would keep the pipe open after the worker exits.
        var child = OperatingSystem.IsWindows() ? "new ProcessStartInfo( \"ping\", \"-n 40 127.0.0.1\" )" : "new ProcessStartInfo( \"sleep\", \"40\" )";

        var code = $"""
                    var startInfo = {child};
                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true;
                    System.Diagnostics.Process.Start( startInfo );
                    Environment.Exit( 3 );
                    return 0;
                    """.Replace( "ProcessStartInfo", "System.Diagnostics.ProcessStartInfo", StringComparison.Ordinal );

        var stopwatch = Stopwatch.StartNew();
        var (isError, text, _) = await server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = code, ["timeoutSeconds"] = 30 } );

        Assert.True( isError, text );
        Assert.Contains( "exited", text, StringComparison.Ordinal );
        Assert.True( stopwatch.Elapsed < TimeSpan.FromSeconds( 15 ), $"The crash was reported after {stopwatch.Elapsed}." );
    }
}
