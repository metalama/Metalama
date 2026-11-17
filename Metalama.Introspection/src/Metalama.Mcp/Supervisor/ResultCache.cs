// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace Metalama.Mcp.Supervisor;

/// <summary>
/// The documents of the html results that the result server shows, by a random identifier, with the least recently used
/// ones evicted. It holds data only: showing a result never runs the query again.
/// </summary>
internal sealed class ResultCache
{
    private readonly int _capacity;
    private readonly Dictionary<string, LinkedListNode<(string Id, JsonObject Document)>> _entries = new();
    private readonly LinkedList<(string Id, JsonObject Document)> _order = new();

    public ResultCache( int capacity = 100 )
    {
        this._capacity = capacity;
    }

    public string Add( JsonObject document )
    {
        var id = Base64Url( RandomNumberGenerator.GetBytes( 12 ) );

        lock ( this._entries )
        {
            this._entries[id] = this._order.AddFirst( (id, document) );

            while ( this._entries.Count > this._capacity )
            {
                this._entries.Remove( this._order.Last!.Value.Id );
                this._order.RemoveLast();
            }
        }

        return id;
    }

    public bool TryGet( string id, [NotNullWhen( true )] out JsonObject? document )
    {
        lock ( this._entries )
        {
            if ( this._entries.TryGetValue( id, out var node ) )
            {
                this._order.Remove( node );
                this._order.AddFirst( node );
                document = node.Value.Document;

                return true;
            }
        }

        document = null;

        return false;
    }

    public static string Base64Url( byte[] bytes ) => System.Buffers.Text.Base64Url.EncodeToString( bytes );
}
