// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Infrastructure;

/// <summary>
/// Tests of <see cref="ProcessExecutor"/>, which starts real processes. Every command used here exists on every
/// operating system on which the tests run.
/// </summary>
public sealed class ProcessExecutorTests : TestsBase
{
    /// <summary>
    /// The time that the process of <see cref="CreateNonTerminatingProcessStartInfo"/> runs when nothing terminates
    /// it. Every assertion about the timeout is verified within a much shorter time than this one, so a test fails
    /// when the timeout is not observed.
    /// </summary>
    private static readonly TimeSpan _nonTerminatingProcessDuration = TimeSpan.FromSeconds( 30 );

    private static readonly TimeSpan _shortTimeout = TimeSpan.FromMilliseconds( 500 );

    private static readonly TimeSpan _longTimeout = TimeSpan.FromSeconds( 60 );

    /// <summary>
    /// Gets a value indicating whether the tests run on Windows. The type is qualified because <c>TestsBase</c>
    /// exposes a property of the same name, which reports the platform simulated by the test.
    /// </summary>
    private static bool IsWindows => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform( OSPlatform.Windows );

    public ProcessExecutorTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Creates the description of a process that runs the given command through the command interpreter of the
    /// current operating system.
    /// </summary>
    private static ProcessStartInfo CreateShellStartInfo( string command )
        => IsWindows
            ? new ProcessStartInfo( "cmd.exe", $"/c {command}" )
            : new ProcessStartInfo( "/bin/sh", $"-c \"{command}\"" );

    /// <summary>
    /// Creates the description of a process that runs for <see cref="_nonTerminatingProcessDuration"/> and that
    /// starts no other process, so that terminating it terminates the whole command.
    /// </summary>
    private static ProcessStartInfo CreateNonTerminatingProcessStartInfo()
        => IsWindows
            ? new ProcessStartInfo( "ping.exe", $"-n {(int) _nonTerminatingProcessDuration.TotalSeconds} 127.0.0.1" )
            : new ProcessStartInfo( "/bin/sh", $"-c \"sleep {(int) _nonTerminatingProcessDuration.TotalSeconds}\"" );

    /// <summary>
    /// Verifies that the method reports the text that the process writes to its standard output.
    /// </summary>
    [Fact]
    public void StandardOutputIsRead()
    {
        Assert.True( new ProcessExecutor().TryReadStandardOutput( CreateShellStartInfo( "echo Metalama" ), _longTimeout, out var standardOutput ) );

        Assert.Equal( "Metalama", standardOutput?.Trim() );
    }

    /// <summary>
    /// Verifies that the method reports a failure when the process returns an exit code other than zero.
    /// </summary>
    [Fact]
    public void NonZeroExitCodeIsReportedAsAFailure()
    {
        Assert.False( new ProcessExecutor().TryReadStandardOutput( CreateShellStartInfo( "exit 3" ), _longTimeout, out var standardOutput ) );

        Assert.Null( standardOutput );
    }

    /// <summary>
    /// Verifies that the method stops waiting when the timeout expires, instead of waiting for the completion of the
    /// process.
    /// </summary>
    /// <remarks>
    /// The method reads the standard output of the process. A blocking read of that stream returns only when the
    /// process closes it, so an implementation that reads before waiting observes no timeout at all. See issue #1873.
    /// </remarks>
    [Fact]
    public void TimeoutIsObservedWhileTheProcessRuns()
    {
        var stopwatch = Stopwatch.StartNew();

        Assert.False( new ProcessExecutor().TryReadStandardOutput( CreateNonTerminatingProcessStartInfo(), _shortTimeout, out var standardOutput ) );

        var elapsed = stopwatch.Elapsed;
        this.Logger.WriteLine( $"The method returned after {elapsed}." );

        Assert.Null( standardOutput );

        // The bound is generous, because the machine that runs the test is shared, but it is far below the time that
        // the process runs when the timeout is not observed.
        Assert.True( elapsed < TimeSpan.FromSeconds( 15 ), $"The method returned after {elapsed}, so it waited for the process." );
    }

    /// <summary>
    /// Verifies that the process is terminated when the timeout expires, instead of being left running without a
    /// parent that waits for it.
    /// </summary>
    /// <remarks>
    /// Windows keeps a handle on the working directory of a running process, so the directory can be deleted only
    /// after the process has ended. The other operating systems do not, so the test is skipped there.
    /// </remarks>
    [SkippableFact]
    public void ProcessIsTerminatedWhenTheTimeoutExpires()
    {
        Skip.IfNot( IsWindows, "A process holds a handle on its working directory only on Windows." );

        var workingDirectory = Path.Combine( Path.GetTempPath(), $"Metalama.ProcessExecutorTests.{Guid.NewGuid():N}" );
        Directory.CreateDirectory( workingDirectory );

        try
        {
            var startInfo = CreateNonTerminatingProcessStartInfo();
            startInfo.WorkingDirectory = workingDirectory;

            Assert.False( new ProcessExecutor().TryReadStandardOutput( startInfo, _shortTimeout, out _ ) );

            // The deletion is attempted repeatedly, because the handle of the process is released a short time after
            // the process has been terminated. The waiting time is far below the time that the process runs when it
            // is not terminated.
            Assert.True(
                SpinWait.SpinUntil( () => TryDeleteDirectory( workingDirectory ), TimeSpan.FromSeconds( 10 ) ),
                "The working directory of the process could not be deleted, so the process was still running." );
        }
        finally
        {
            _ = TryDeleteDirectory( workingDirectory );
        }
    }

    private static bool TryDeleteDirectory( string directory )
    {
        try
        {
            if ( Directory.Exists( directory ) )
            {
                Directory.Delete( directory, true );
            }

            return true;
        }
        catch ( IOException )
        {
            // The directory is still the working directory of a running process.
            return false;
        }
        catch ( UnauthorizedAccessException )
        {
            // The directory is still the working directory of a running process.
            return false;
        }
    }
}
