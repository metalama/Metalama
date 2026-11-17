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
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Mcp.Supervisor;

/// <summary>
/// Starts the worker process, sends it requests one at a time, and enforces timeouts. A request that outlives the hard
/// timeout gets the worker killed; the next request starts a new worker and reloads the workspaces it had.
/// </summary>
/// <remarks>
/// The worker runs arbitrary code, so nothing it sends is trusted: any response that is not a well-formed message with the
/// identifier of the request is a failure of the worker, which is then stopped.
/// </remarks>
internal sealed class WorkerClient : IAsyncDisposable
{
    private static readonly TimeSpan _loadTimeout = TimeSpan.FromMinutes( 15 );

    // How long a response may still arrive after the worker process exited, for a worker that exits right after writing it.
    private static readonly TimeSpan _exitGracePeriod = TimeSpan.FromSeconds( 1 );

    private readonly CommandLineOptions _options;
    private readonly SemaphoreSlim _gate = new( 1, 1 );

    // The load requests of the loaded workspaces, by workspace key, so that they can be replayed after a restart.
    private readonly Dictionary<string, JsonObject> _loads = new( StringComparer.Ordinal );

    private readonly object _processLock = new();
    private Process? _process;
    private MessageChannel? _channel;
    private int _nextId;
    private bool _disposed;

    public WorkerClient( CommandLineOptions options )
    {
        this._options = options;
    }

    public async Task<JsonObject> CallAsync( string method, JsonObject args, TimeSpan? timeout, CancellationToken cancellationToken )
    {
        await this._gate.WaitAsync( cancellationToken );

        try
        {
            ObjectDisposedException.ThrowIf( this._disposed, this );

            var (restartNote, restartError) = await this.EnsureStartedAsync( cancellationToken );

            if ( restartError != null )
            {
                return restartError;
            }

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
        MessageChannel channel;
        Process process;

        lock ( this._processLock )
        {
            if ( this._channel == null || this._process == null )
            {
                return Error( 0, "The worker process is not running. The next call starts a new one." );
            }

            channel = this._channel;
            process = this._process;
        }

        var id = ++this._nextId;

        try
        {
            await channel.WriteAsync( new JsonObject { [WorkerMethods.Id] = id, [WorkerMethods.Method] = method, [WorkerMethods.Args] = args.DeepClone() }, cancellationToken );
        }
        catch ( Exception e ) when ( e is IOException or ObjectDisposedException )
        {
            return this.FailWorker( id, "The worker process cannot receive requests. The next call starts a new worker and reloads the workspaces." );
        }

        var responseTask = channel.ReadAsync( CancellationToken.None );
        var exitTask = process.WaitForExitAsync( CancellationToken.None );

        using var cancelOnRequest = cancellationToken.Register( () => _ = SendCancelAsync( channel ) );

        if ( !await WaitForResponseAsync( responseTask, exitTask, softTimeout ) )
        {
            if ( !exitTask.IsCompleted )
            {
                // Soft timeout: ask the query to stop through its cancellation token.
                await SendCancelAsync( channel );

                if ( !await WaitForResponseAsync( responseTask, exitTask, softTimeout ) && !exitTask.IsCompleted )
                {
                    // Hard timeout: the query does not observe its cancellation token, so the worker is killed.
                    return this.FailWorker(
                        id,
                        $"The request exceeded the hard timeout of {(softTimeout * 2).TotalSeconds.ToString( CultureInfo.InvariantCulture )} s and did not observe its cancellation token `ct`, so the worker process was stopped. "
                        + "The next call starts a new worker and reloads the workspaces, which takes as long as the first load." );
                }
            }
        }

        if ( !responseTask.IsCompleted )
        {
            // The worker exited without answering. Its pipe may stay open in a process it started, so its exit is the signal.
            await Task.WhenAny( responseTask, Task.Delay( _exitGracePeriod, CancellationToken.None ) );
        }

        JsonObject? response = null;
        string? failure = null;

        if ( responseTask.IsCompleted )
        {
            try
            {
                response = await responseTask;
            }
            catch ( Exception e ) when ( e is not OutOfMemoryException )
            {
                // A malformed frame: an invalid length, invalid JSON, or JSON that is not an object.
                failure = $"The worker process sent a malformed response ({e.GetType().Name}: {e.Message})";
            }
        }

        if ( failure == null && response == null )
        {
            var exitCode = exitTask.IsCompleted ? process.ExitCode.ToString( CultureInfo.InvariantCulture ) : "unknown";
            failure = $"The worker process exited unexpectedly (exit code {exitCode})";
        }

        if ( failure == null && !IsResponseTo( response!, id ) )
        {
            // Code running in the worker can write to the pipe, so a frame for another request is not trusted.
            failure = "The worker process sent a response that does not match the request";
        }

        if ( failure != null )
        {
            return this.FailWorker( id, failure + ", so it was stopped. The next call starts a new worker and reloads the workspaces." );
        }

        return response!;
    }

    private static async Task<bool> WaitForResponseAsync( Task<JsonObject?> responseTask, Task exitTask, TimeSpan timeout )
    {
        var completed = await Task.WhenAny( responseTask, exitTask, Task.Delay( timeout, CancellationToken.None ) );

        return completed == responseTask;
    }

    private static bool IsResponseTo( JsonObject response, int id )
    {
        try
        {
            return response[WorkerMethods.Id]?.GetValue<int>() == id && response[WorkerMethods.Ok] is JsonValue;
        }
        catch ( Exception e ) when ( e is InvalidOperationException or FormatException )
        {
            return false;
        }
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
    /// Starts the worker if it does not run, and replays the loads of the previous worker. Returns a note for the user about
    /// the workspaces that were reloaded or not, or an error when the new worker failed too.
    /// </summary>
    private async Task<(string? Note, JsonObject? Error)> EnsureStartedAsync( CancellationToken cancellationToken )
    {
        lock ( this._processLock )
        {
            if ( this._process is { HasExited: false } && this._channel != null )
            {
                return (null, null);
            }
        }

        this.KillWorker();
        this.StartWorker();

        if ( this._loads.Count == 0 )
        {
            return (null, null);
        }

        var reloaded = new List<string>();
        var failed = new List<string>();

        foreach ( var (key, loadArgs) in this._loads.ToList() )
        {
            var response = await this.SendAsync( WorkerMethods.Load, loadArgs, _loadTimeout, cancellationToken );

            if ( response[WorkerMethods.Ok]?.GetValue<bool>() == true )
            {
                reloaded.Add( key );

                continue;
            }

            this._loads.Remove( key );
            failed.Add( $"{key} ({response[WorkerMethods.Error]?.GetValue<string>()})" );

            bool workerStopped;

            lock ( this._processLock )
            {
                workerStopped = this._channel == null;
            }

            if ( workerStopped )
            {
                // The new worker failed while it reloaded: the remaining workspaces cannot be reloaded either.
                failed.AddRange( this._loads.Keys );
                this._loads.Clear();

                return (null, Error( 0, "The worker was restarted, but it failed while it reloaded the workspaces: " + string.Join( "; ", failed ) ));
            }
        }

        var note = "note: the worker was restarted.";

        if ( reloaded.Count > 0 )
        {
            note += $" Reloaded: {string.Join( ", ", reloaded )}.";
        }

        if ( failed.Count > 0 )
        {
            note += $" These workspaces could not be reloaded and were unloaded: {string.Join( "; ", failed )}.";
        }

        return (note, null);
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
        if ( this._options.OutputDirectory != null )
        {
            startInfo.ArgumentList.Add( CommandLineOptions.OutputDirectoryOption );
            startInfo.ArgumentList.Add( this._options.OutputDirectory );
        }

        startInfo.ArgumentList.Add( CommandLineOptions.EditorLinkOption );
        startInfo.ArgumentList.Add( this._options.EditorLink ?? "none" );

        var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += ( _, e ) => ForwardToStandardError( e.Data );
        process.ErrorDataReceived += ( _, e ) => ForwardToStandardError( e.Data );
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.StandardInput.Close();

        toWorker.DisposeLocalCopyOfClientHandle();
        fromWorker.DisposeLocalCopyOfClientHandle();

        lock ( this._processLock )
        {
            this._process = process;
            this._channel = new MessageChannel( fromWorker, toWorker );
        }

        Console.Error.WriteLine( $"metalama-mcp: supervisor pid={Environment.ProcessId.ToString( CultureInfo.InvariantCulture )} started worker pid={process.Id.ToString( CultureInfo.InvariantCulture )}" );
    }

    private static void ForwardToStandardError( string? line )
    {
        if ( line != null )
        {
            Console.Error.WriteLine( "worker: " + line );
        }
    }

    private JsonObject FailWorker( int id, string message )
    {
        this.KillWorker();

        return Error( id, message );
    }

    private void KillWorker()
    {
        Process? process;
        MessageChannel? channel;

        lock ( this._processLock )
        {
            process = this._process;
            channel = this._channel;
            this._process = null;
            this._channel = null;
        }

        channel?.Dispose();

        if ( process != null )
        {
            try
            {
                if ( !process.HasExited )
                {
                    // The whole tree, so that the MSBuild build host goes with the worker.
                    process.Kill( entireProcessTree: true );
                    process.WaitForExit( 10_000 );
                }
            }
            catch ( InvalidOperationException )
            {
                // The process already exited.
            }

            process.Dispose();
        }
    }

    private static JsonObject Error( int id, string message )
        => new() { [WorkerMethods.Id] = id, [WorkerMethods.Ok] = false, [WorkerMethods.Error] = message, [WorkerMethods.Text] = "error: " + message };

    public ValueTask DisposeAsync()
    {
        // The gate is not awaited: a call in flight may hold it for the whole hard timeout, and the client may terminate the
        // supervisor before. The worker also exits by itself when its pipe from the supervisor closes.
        this._disposed = true;
        this.KillWorker();

        return ValueTask.CompletedTask;
    }
}
