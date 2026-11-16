// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Mcp.Protocol;

/// <summary>
/// Exchanges JSON messages between the supervisor and the worker over a pair of anonymous pipes. Each message is a
/// four-byte little-endian length followed by UTF-8 JSON.
/// </summary>
internal sealed class MessageChannel : IDisposable
{
    public const int DefaultMaxMessageLength = 256 * 1024 * 1024;

    private readonly Stream _input;
    private readonly Stream _output;
    private readonly int _maxMessageLength;
    private readonly SemaphoreSlim _writeLock = new( 1, 1 );

    public MessageChannel( Stream input, Stream output, int maxMessageLength = DefaultMaxMessageLength )
    {
        this._input = input;
        this._output = output;
        this._maxMessageLength = maxMessageLength;
    }

    /// <summary>
    /// Writes a message.
    /// </summary>
    /// <exception cref="InvalidDataException">The message is larger than the limit that the reader enforces. Nothing is written.</exception>
    public async Task WriteAsync( JsonObject message, CancellationToken cancellationToken = default )
    {
        var payload = Encoding.UTF8.GetBytes( message.ToJsonString() );

        if ( payload.Length > this._maxMessageLength )
        {
            throw new InvalidDataException( $"The message is {payload.Length} bytes long, above the limit of {this._maxMessageLength} bytes." );
        }

        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian( header, payload.Length );

        await this._writeLock.WaitAsync( cancellationToken );

        try
        {
            await this._output.WriteAsync( header, cancellationToken );
            await this._output.WriteAsync( payload, cancellationToken );
            await this._output.FlushAsync( cancellationToken );
        }
        finally
        {
            this._writeLock.Release();
        }
    }

    /// <summary>
    /// Reads the next message, or returns <c>null</c> when the other process closed its end of the pipe.
    /// </summary>
    public async Task<JsonObject?> ReadAsync( CancellationToken cancellationToken = default )
    {
        var header = new byte[4];

        if ( !await this.ReadExactlyAsync( header, cancellationToken ) )
        {
            return null;
        }

        var length = BinaryPrimitives.ReadInt32LittleEndian( header );

        if ( length < 0 || length > this._maxMessageLength )
        {
            throw new InvalidDataException( $"Invalid message length {length}." );
        }

        var payload = new byte[length];

        if ( !await this.ReadExactlyAsync( payload, cancellationToken ) )
        {
            return null;
        }

        return (JsonObject) JsonNode.Parse( payload )!;
    }

    private async Task<bool> ReadExactlyAsync( byte[] buffer, CancellationToken cancellationToken )
    {
        var offset = 0;

        while ( offset < buffer.Length )
        {
            var read = await this._input.ReadAsync( buffer.AsMemory( offset ), cancellationToken );

            if ( read == 0 )
            {
                return false;
            }

            offset += read;
        }

        return true;
    }

    public void Dispose()
    {
        this._input.Dispose();
        this._output.Dispose();
        this._writeLock.Dispose();
    }
}
