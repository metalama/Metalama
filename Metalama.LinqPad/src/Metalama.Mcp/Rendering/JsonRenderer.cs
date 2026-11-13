// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Workspaces.Navigation.Links;
using Metalama.Framework.Workspaces.Navigation.Results;
using System.Linq;
using System.Text.Json.Nodes;

namespace Metalama.Mcp.Rendering;

/// <summary>
/// Renders a result tree as JSON, following <c>result.schema.json</c> (schema version 1). This is also the input of the HTML viewer.
/// </summary>
internal sealed class JsonRenderer
{
    public const int SchemaVersion = 1;

    private readonly LinkBase? _base;

    public JsonRenderer( LinkBase? linkBase )
    {
        this._base = linkBase;
    }

    /// <summary>
    /// Renders the document: the schema version, the base, and the result.
    /// </summary>
    public JsonObject RenderDocument( ResultNode node, string? title = null )
    {
        var document = new JsonObject { ["schema"] = SchemaVersion, ["base"] = this._base?.ToString(), ["result"] = this.Render( node ) };

        if ( title != null )
        {
            document["title"] = title;
        }

        return document;
    }

    public JsonObject Render( ResultNode node )
    {
        var json = node switch
        {
            ScalarNode scalar => new JsonObject { ["kind"] = "scalar", ["type"] = scalar.Kind.ToString().ToLowerInvariant(), ["value"] = scalar.Text },
            ObjectNode obj => this.RenderObject( obj ),
            CollectionNode collection => this.RenderCollection( collection ),
            GroupingNode grouping => new JsonObject { ["kind"] = "grouping", ["key"] = this.Render( grouping.Key ), ["items"] = this.RenderCollection( grouping.Items ) },
            ErrorNode error => new JsonObject { ["kind"] = "error", ["exception"] = error.ExceptionType, ["message"] = error.Message },
            RichTextNode text => new JsonObject
            {
                ["kind"] = "text",
                ["text"] = text.Text,
                ["segments"] = new JsonArray(
                    text.Segments.Select(
                            s => (JsonNode) (s.Argument != null
                                ? new JsonObject { ["text"] = s.Text, ["node"] = this.Render( s.Argument ) }
                                : new JsonObject { ["text"] = s.Text }) )
                        .ToArray() )
            },
            _ => new JsonObject { ["kind"] = "scalar", ["type"] = "other", ["value"] = node.ToString() }
        };

        if ( node.Link != null )
        {
            json["link"] = node.Link.ToRelativeString( this._base );
            json["href"] = node.Link.ToAbsoluteString();
        }

        return json;
    }

    private JsonObject RenderObject( ObjectNode obj )
    {
        var json = new JsonObject { ["kind"] = "object", ["type"] = obj.TypeName, ["display"] = obj.Display };

        if ( obj.IsDeclaration )
        {
            json["declaration"] = true;
        }

        if ( obj.IsTransformed )
        {
            json["transformed"] = true;
        }

        if ( obj.Location != null )
        {
            json["location"] = obj.Location;
        }

        if ( obj.InterfaceName != null && obj.Link is { Steps.IsEmpty: true, IsIdentifierAnchor: true } link )
        {
            json["expression"] = $"D<{obj.InterfaceName}>(\"{link.Anchor}\")";
        }

        if ( obj.Properties is { } properties )
        {
            json["properties"] = new JsonArray(
                properties.Select( p => (JsonNode) new JsonObject { ["name"] = p.Name, ["value"] = this.Render( p.Value ) } ).ToArray() );
        }

        return json;
    }

    private JsonObject RenderCollection( CollectionNode collection )
    {
        var json = new JsonObject { ["kind"] = "collection", ["type"] = collection.TypeName, ["count"] = collection.Count };

        if ( collection.IsTruncated )
        {
            json["truncated"] = true;
        }

        if ( collection.Items is { } items )
        {
            json["items"] = new JsonArray( items.Select( i => (JsonNode) this.Render( i ) ).ToArray() );
        }

        if ( collection.Link != null )
        {
            json["link"] = collection.Link.ToRelativeString( this._base );
            json["href"] = collection.Link.ToAbsoluteString();
        }

        return json;
    }
}
