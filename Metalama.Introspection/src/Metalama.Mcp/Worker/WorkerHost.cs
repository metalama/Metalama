// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Mcp.Protocol;
using System;
using System.IO.Pipes;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Metalama.Mcp.Worker;

/// <summary>
/// The main loop of the worker: it reads requests from the supervisor and runs them one at a time. A <c>cancel</c> message
/// cancels the request in progress. The worker exits when the supervisor closes the pipe.
/// </summary>
internal static class WorkerHost
{
    public static async Task<int> RunAsync( CommandLineOptions options )
    {
        using var input = new AnonymousPipeClientStream( PipeDirection.In, options.WorkerInputHandle! );
        using var output = new AnonymousPipeClientStream( PipeDirection.Out, options.WorkerOutputHandle! );
        using var channel = new MessageChannel( input, output );

        WorkerInitialization.Initialize();

        var handler = new RequestHandler( options );
        var requests = Channel.CreateUnbounded<JsonObject>();
        CancellationTokenSource? current = null;
        var currentLock = new object();

        var reader = Task.Run(
            async () =>
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

                requests.Writer.Complete();
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

            await channel.WriteAsync( response );
        }

        await reader;

        return 0;
    }
}
