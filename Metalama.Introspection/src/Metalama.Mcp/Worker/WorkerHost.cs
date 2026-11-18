// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Mcp.Protocol;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Metalama.Mcp.Worker;

/// <summary>
/// The main loop of the worker: it reads requests from the supervisor and runs them one at a time. A <c>cancel</c> message
/// cancels the request in progress. The worker exits as soon as the pipe from the supervisor closes, even during a request.
/// </summary>
internal static class WorkerHost
{
    public static async Task<int> RunAsync( CommandLineOptions options )
    {
        using var input = new AnonymousPipeClientStream( PipeDirection.In, options.WorkerInputHandle! );
        using var output = new AnonymousPipeClientStream( PipeDirection.Out, options.WorkerOutputHandle! );

        // The processes that the worker starts, such as the MSBuild build host or a process started by a query, must not
        // inherit the pipes: they could write to the supervisor, and they would keep the pipe open after the worker exits.
        HandleInheritance.Disable( input.SafePipeHandle );
        HandleInheritance.Disable( output.SafePipeHandle );

        using var channel = new MessageChannel( input, output );

        WorkerInitialization.Initialize();

        using var handler = new RequestHandler( options );
        var requests = Channel.CreateUnbounded<JsonObject>();
        CancellationTokenSource? current = null;
        var currentLock = new object();

        _ = Task.Run(
            async () =>
            {
                try
                {
                    while ( await channel.ReadAsync() is { } message )
                    {
                        if ( message[WorkerMethods.Method]?.GetValue<string>() == WorkerMethods.Cancel )
                        {
                            lock ( currentLock )
                            {
                                current?.Cancel();
                            }
                        }
                        else
                        {
                            await requests.Writer.WriteAsync( message );
                        }
                    }
                }
                catch ( Exception e ) when ( e is not OutOfMemoryException )
                {
                    await Console.Error.WriteLineAsync( $"The pipe from the supervisor failed: {e.Message}" );
                }

                // The supervisor is gone. A request in progress, such as a query that never ends, must not keep the worker
                // and its build host running without a parent.
                Environment.Exit( 0 );
            } );

        await foreach ( var request in requests.Reader.ReadAllAsync() )
        {
            using var cancellation = new CancellationTokenSource();

            lock ( currentLock )
            {
                current = cancellation;
            }

            JsonObject response;

            try
            {
                response = await handler.HandleAsync( request, cancellation.Token );
            }
            finally
            {
                lock ( currentLock )
                {
                    current = null;
                }
            }

            response[WorkerMethods.Id] = request[WorkerMethods.Id]?.DeepClone();

            // Responses use \n on every platform.
            if ( response[WorkerMethods.Text]?.GetValue<string>() is { } text )
            {
                response[WorkerMethods.Text] = text.Replace( "\r\n", "\n", StringComparison.Ordinal );
            }

            try
            {
                await channel.WriteAsync( response );
            }
            catch ( InvalidDataException e )
            {
                // The response is above the size limit of the pipe, for example a JSON result with a large maxItems.
                var message =
                    $"The result is too large to return ({e.Message}). Narrow the query, or lower maxItems or maxDepth.";

                await channel.WriteAsync(
                    new JsonObject
                    {
                        [WorkerMethods.Id] = request[WorkerMethods.Id]?.DeepClone(),
                        [WorkerMethods.Ok] = false,
                        [WorkerMethods.Error] = message,
                        [WorkerMethods.Text] = "error: " + message
                    } );
            }
        }

        return 0;
    }
}
