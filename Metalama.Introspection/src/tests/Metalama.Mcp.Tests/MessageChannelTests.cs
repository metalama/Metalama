// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Mcp.Protocol;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Mcp.Tests;

public sealed class MessageChannelTests
{
    [Fact]
    public async Task MessagesRoundTrip()
    {
        using var pipe = new MemoryStream();
        using var writer = new MessageChannel( new MemoryStream(), pipe );
        await writer.WriteAsync( new JsonObject { ["id"] = 1, ["text"] = "hello" }, TestContext.Current.CancellationToken );

        pipe.Position = 0;
        using var reader = new MessageChannel( pipe, new MemoryStream() );
        var message = await reader.ReadAsync( TestContext.Current.CancellationToken );

        Assert.Equal( "hello", message!["text"]!.GetValue<string>() );
    }

    [Fact]
    public async Task OversizedMessageIsNotWritten()
    {
        using var pipe = new MemoryStream();
        using var channel = new MessageChannel( new MemoryStream(), pipe, maxMessageLength: 100 );

        // The writer enforces the limit that the reader enforces, so that the reader never desynchronizes.
        await Assert.ThrowsAsync<InvalidDataException>(
            () => channel.WriteAsync( new JsonObject { ["text"] = new string( 'x', 200 ) }, TestContext.Current.CancellationToken ) );

        Assert.Equal( 0, pipe.Length );
    }
}
