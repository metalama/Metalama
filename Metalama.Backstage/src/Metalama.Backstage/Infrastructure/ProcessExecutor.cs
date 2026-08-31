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

        using var process = Process.Start( startInfo );

        if ( process == null )
        {
            return false;
        }

        // The error stream is drained, otherwise a full error buffer would block the child process while this method
        // reads its output stream.
        process.ErrorDataReceived += ( _, _ ) => { };
        process.BeginErrorReadLine();

        var output = process.StandardOutput.ReadToEnd();

        if ( !process.WaitForExit( (int) timeout.TotalMilliseconds ) || process.ExitCode != 0 )
        {
            return false;
        }

        standardOutput = output;

        return true;
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
