// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using System;
using System.Diagnostics;
using System.Threading;

namespace Metalama.Backstage.Maintenance;

internal abstract partial class ProcessManagerBase
{
    protected class KillableProcess
    {
        private readonly ILogger _logger;

        public KillableProcessSpec Spec { get; }

        public Process Process { get; }

        public string? MainModule { get; }

        private bool Shutdown()
        {
            try
            {
                if ( this.Process.HasExited )
                {
                    return true;
                }

                this._logger.Trace?.Log( $"Gracefully shutting down process {this.Process.Id}." );

                var shutdownProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = this.Process.MainModule!.FileName,
                        Arguments = this.MainModule != null ? $"\"{this.MainModule}\" -shutdown" : "-shutdown",
                        RedirectStandardOutput = true
                    }
                };

                shutdownProcess.Start();
                shutdownProcess.WaitForExit();

                var waitCycles = 0;

                while ( !this.Process.HasExited )
                {
                    if ( waitCycles > 5 )
                    {
                        return false;
                    }

                    waitCycles++;

                    Thread.Sleep( 1 );
                }

                return true;
            }
            catch ( Exception e )
            {
                this._logger.Warning?.Log( $"Unable to gracefully shut down process {this.Process.Id}: {e.Message}" );

                return false;
            }
        }

        private void Kill()
        {
            var process = this.Process;

            if ( process.HasExited )
            {
                return;
            }

            try
            {
                this._logger.Trace?.Log( $"Killing process '{process.ProcessName}' (PID: {process.Id})." );

                process.Kill();
                process.WaitForExit();
            }
            catch ( InvalidOperationException ) when ( process.HasExited )
            {
                // Nothing to do. We lost a race that ended the process.   
            }
            catch ( Exception e )
            {
                this._logger.Error?.Log( $"Could not kill process '{process.ProcessName}' (PID: {process.Id}): {e.Message}." );
            }
        }

        public KillableProcess( Process process, ILogger logger, string? mainModule, KillableProcessSpec spec )
        {
            this.Process = process;
            this._logger = logger;
            this.Spec = spec;
            this.MainModule = mainModule;
        }

        public void ShutdownOrKill()
        {
            if ( this.Spec.CanShutdown )
            {
                if ( this.Shutdown() )
                {
                    return;
                }
            }

            this.Kill();
        }
    }
}