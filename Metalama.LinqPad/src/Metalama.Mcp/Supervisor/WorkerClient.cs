// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Mcp.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Mcp.Supervisor;

/// <summary>
/// Starts the worker process, sends it requests one at a time, and enforces timeouts. A request that outlives the hard
/// timeout gets the worker killed; the next request starts a new worker and reloads the workspaces it had.
/// </summary>
internal sealed class WorkerClient : IAsyncDisposable
{
    private static readonly TimeSpan _loadTimeout = TimeSpan.FromMinutes( 15 );

    private readonly CommandLineOptions _options;
    private readonly SemaphoreSlim _gate = new( 1, 1 );

    // The load requests of the loaded workspaces, by workspace key, so that they can be replayed after a restart.
    private readonly Dictionary<string, JsonObject> _loads = new( StringComparer.Ordinal );

    private Process? _process;
    private MessageChannel? _channel;
    private int _nextId;

    public WorkerClient( CommandLineOptions options )
    {
        this._options = options;
    }

    /// <summary>
    /// Gets the process identifier of the current worker, or <c>null</c> when no worker runs.
    /// </summary>
    public int? WorkerProcessId => this._process is { HasExited: false } process ? process.Id : null;

    public async Task<JsonObject> CallAsync( string method, JsonObject args, TimeSpan? timeout, CancellationToken cancellationToken )
    {
        await this._gate.WaitAsync( cancellationToken );

        try
        {
            var restartNote = await this.EnsureStartedAsync( cancellationToken );
            var response = await this.SendAsync( method, args, timeout ?? (method == WorkerMethods.Load ? _loadTimeout : this._options.DefaultTimeout), cancellationToken );

            if ( response[WorkerMethods.Ok]?.GetValue<bool>() == true )
            {
                var workspace = response[WorkerMethods.Workspace]?.GetValue<string>();

                switch ( method )
                {
                    case WorkerMethods.Load when workspace != null:
                        this._loads[workspace] = (JsonObject) args.DeepClone();

                        break;

                    case WorkerMethods.Unload when workspace != null:
                        this._loads.Remove( workspace );

                        break;
                }
            }

            if ( restartNote != null && response[WorkerMethods.Text] is { } text )
            {
                response[WorkerMethods.Text] = restartNote + "\n" + text.GetValue<string>();
            }

            return response;
        }
        finally
        {
            this._gate.Release();
        }
    }

    private async Task<JsonObject> SendAsync( string method, JsonObject args, TimeSpan softTimeout, CancellationToken cancellationToken )
    {
        var channel = this._channel!;
        var id = ++this._nextId;

        await channel.WriteAsync( new JsonObject { [WorkerMethods.Id] = id, [WorkerMethods.Method] = method, [WorkerMethods.Args] = args.DeepClone() }, cancellationToken );

        var responseTask = channel.ReadAsync( CancellationToken.None );

        using var cancelOnRequest = cancellationToken.Register( () => _ = SendCancelAsync( channel ) );

        if ( await Task.WhenAny( responseTask, Task.Delay( softTimeout, CancellationToken.None ) ) != responseTask )
        {
            // Soft timeout: ask the query to stop through its cancellation token.
            await SendCancelAsync( channel );

            if ( await Task.WhenAny( responseTask, Task.Delay( softTimeout, CancellationToken.None ) ) != responseTask )
            {
                // Hard timeout: the query does not observe its cancellation token, so the worker is killed.
                this.KillWorker();

                return Error(
                    id,
                    $"The request exceeded the hard timeout of {(softTimeout * 2).TotalSeconds.ToString( CultureInfo.InvariantCulture )} s and did not observe its cancellation token `ct`, so the worker process was stopped. "
                    + "The next call starts a new worker and reloads the workspaces, which takes as long as the first load." );
            }
        }

        JsonObject? response;

        try
        {
            response = await responseTask;
        }
        catch ( Exception e ) when ( e is IOException or ObjectDisposedException )
        {
            response = null;
        }

        if ( response == null )
        {
            var exitCode = this._process is { HasExited: true } process ? process.ExitCode.ToString( CultureInfo.InvariantCulture ) : "unknown";
            this.KillWorker();

            return Error( id, $"The worker process exited unexpectedly (exit code {exitCode}). The next call starts a new worker and reloads the workspaces." );
        }

        return response;
    }

    private static async Task SendCancelAsync( MessageChannel channel )
    {
        try
        {
            await channel.WriteAsync( new JsonObject { [WorkerMethods.Method] = WorkerMethods.Cancel } );
        }
        catch ( Exception e ) when ( e is IOException or ObjectDisposedException )
        {
            // The worker is gone.
        }
    }

    /// <summary>
    /// Starts the worker if it does not run, and replays the loads of the previous worker. Returns a note for the user when
    /// workspaces were reloaded.
    /// </summary>
    private async Task<string?> EnsureStartedAsync( CancellationToken cancellationToken )
    {
        if ( this._process is { HasExited: false } && this._channel != null )
        {
            return null;
        }

        this.KillWorker();
        this.StartWorker();

        if ( this._loads.Count == 0 )
        {
            return null;
        }

        var reloaded = new List<string>();

        foreach ( var (key, loadArgs) in new Dictionary<string, JsonObject>( this._loads ) )
        {
            var response = await this.SendAsync( WorkerMethods.Load, loadArgs, _loadTimeout, cancellationToken );

            if ( response[WorkerMethods.Ok]?.GetValue<bool>() == true )
            {
                reloaded.Add( key );
            }
            else
            {
                this._loads.Remove( key );
            }
        }

        return $"note: the worker was restarted and reloaded {string.Join( ", ", reloaded )}.";
    }

    private void StartWorker()
    {
        var toWorker = new AnonymousPipeServerStream( PipeDirection.Out, HandleInheritability.Inheritable );
        var fromWorker = new AnonymousPipeServerStream( PipeDirection.In, HandleInheritability.Inheritable );

        var processPath = Environment.ProcessPath ?? throw new InvalidOperationException( "Cannot determine the path of the current process." );

        var startInfo = new ProcessStartInfo( processPath )
        {
            UseShellExecute = false,

            // The worker must not inherit the standard input of the supervisor, which carries the MCP stream, and its
            // output goes to the standard error of the supervisor, never to its standard output.
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if ( string.Equals( Path.GetFileNameWithoutExtension( processPath ), "dotnet", StringComparison.OrdinalIgnoreCase ) )
        {
            startInfo.ArgumentList.Add( typeof(WorkerClient).Assembly.Location );
        }

        startInfo.ArgumentList.Add( CommandLineOptions.WorkerOption );
        startInfo.ArgumentList.Add( toWorker.GetClientHandleAsString() );
        startInfo.ArgumentList.Add( fromWorker.GetClientHandleAsString() );
        startInfo.ArgumentList.Add( CommandLineOptions.OutputDirectoryOption );
        startInfo.ArgumentList.Add( this._options.OutputDirectory );

        var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += ( _, e ) => ForwardToStandardError( e.Data );
        process.ErrorDataReceived += ( _, e ) => ForwardToStandardError( e.Data );
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.StandardInput.Close();

        toWorker.DisposeLocalCopyOfClientHandle();
        fromWorker.DisposeLocalCopyOfClientHandle();

        this._process = process;
        this._channel = new MessageChannel( fromWorker, toWorker );

        Console.Error.WriteLine( $"metalama-mcp: supervisor pid={Environment.ProcessId.ToString( CultureInfo.InvariantCulture )} started worker pid={process.Id.ToString( CultureInfo.InvariantCulture )}" );
    }

    private static void ForwardToStandardError( string? line )
    {
        if ( line != null )
        {
            Console.Error.WriteLine( "worker: " + line );
        }
    }

    private void KillWorker()
    {
        this._channel?.Dispose();
        this._channel = null;

        if ( this._process != null )
        {
            try
            {
                if ( !this._process.HasExited )
                {
                    // The whole tree, so that the MSBuild build host goes with the worker.
                    this._process.Kill( entireProcessTree: true );
                    this._process.WaitForExit( 10_000 );
                }
            }
            catch ( InvalidOperationException )
            {
                // The process already exited.
            }

            this._process.Dispose();
            this._process = null;
        }
    }

    private static JsonObject Error( int id, string message )
        => new() { [WorkerMethods.Id] = id, [WorkerMethods.Ok] = false, [WorkerMethods.Error] = message, [WorkerMethods.Text] = "error: " + message };

    public async ValueTask DisposeAsync()
    {
        await this._gate.WaitAsync();

        try
        {
            // Closing the pipe makes the worker exit; killing covers a worker stuck in a query.
            this._channel?.Dispose();
            this._channel = null;

            if ( this._process is { HasExited: false } process && !process.WaitForExit( 2_000 ) )
            {
                this.KillWorker();
            }

            this._process?.Dispose();
            this._process = null;
        }
        finally
        {
            this._gate.Release();
        }
    }
}
