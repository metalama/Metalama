// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Infrastructure;

internal sealed class ProcessExecutor : IProcessExecutor
{
    public IProcess Start( ProcessStartInfo startInfo )
    {
        ResetInheritedEnvironment( startInfo );

        return new ProcessWrapper( Process.Start( startInfo ) ?? throw new InvalidOperationException( "The process could not be started." ) );
    }

    public bool TryReadStandardOutput( ProcessStartInfo startInfo, TimeSpan timeout, [NotNullWhen( true )] out string? standardOutput )
    {
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;

        ResetInheritedEnvironment( startInfo );

        standardOutput = null;

        var stopwatch = Stopwatch.StartNew();

        using var process = Process.Start( startInfo );

        if ( process == null )
        {
            return false;
        }

        // The output stream is read asynchronously. A blocking read returns only when the process closes the stream,
        // so it would make this method wait for the completion of the process for an unbounded time and the timeout
        // would have no effect.
        var outputTask = process.StandardOutput.ReadToEndAsync();

        // The error stream is drained, otherwise a full error buffer would block the child process while this method
        // reads its output stream.
        process.ErrorDataReceived += ( _, _ ) => { };
        process.BeginErrorReadLine();

        if ( !process.WaitForExit( GetRemainingMilliseconds( timeout, stopwatch ) ) )
        {
            Terminate( process );

            return false;
        }

        // The process closes its output stream when it exits, so the read completes, but it can complete after
        // WaitForExit has returned.
        if ( !outputTask.Wait( GetRemainingMilliseconds( timeout, stopwatch ) ) )
        {
            return false;
        }

        if ( process.ExitCode != 0 )
        {
            return false;
        }

        standardOutput = outputTask.Result;

        return true;
    }

    /// <summary>
    /// Gets the part of the timeout that has not elapsed yet, in milliseconds, or zero when the timeout has expired.
    /// </summary>
    private static int GetRemainingMilliseconds( TimeSpan timeout, Stopwatch stopwatch )
    {
        var remaining = timeout - stopwatch.Elapsed;

        if ( remaining <= TimeSpan.Zero )
        {
            return 0;
        }

        return (int) Math.Min( remaining.TotalMilliseconds, int.MaxValue );
    }

    /// <summary>
    /// Terminates a process that has exceeded its timeout. Disposing the <see cref="Process"/> object closes the
    /// handle that this process holds, but it does not stop the child process, so a child process that is not
    /// terminated here would keep running after the caller has given up on it.
    /// </summary>
    private static void Terminate( Process process )
    {
        try
        {
            process.Kill();
        }
        catch ( Exception )
        {
            // The child process can exit between the expiration of the timeout and this call, and the caller has
            // already given up on its output, so a failure to terminate it is not reported.
        }
    }

    private static void ResetInheritedEnvironment( ProcessStartInfo startInfo )
    {
        if ( !startInfo.UseShellExecute )
        {
            // Reset a few environment variables set by the Visual Studio process.
            startInfo.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "";
            startInfo.Environment["DOTNET_ROOT"] = "";
            startInfo.Environment["DOTNET_STARTUP_HOOKS"] = "";
            startInfo.Environment["DOTNET_TC_CallCountThreshold"] = "";
        }
        else
        {
            // We can't set environment variables with ShellExecute=true and this is also probably useless.
        }
    }

    private sealed class ProcessWrapper : IProcess
    {
        private readonly Process _process;

        public int ExitCode => this._process.ExitCode;

        public ProcessWrapper( Process process )
        {
            this._process = process;
            process.Exited += this.OnExited;
        }

        private void OnExited( object? sender, EventArgs e )
        {
            this.Exited?.Invoke();
        }

        public event Action? Exited;

        public bool HasExited => this._process.HasExited;

        public void WaitForExit() => this._process.WaitForExit();

        public void Dispose()
        {
            this._process.Exited -= this.OnExited;
            this._process.Dispose();
        }
    }
}
