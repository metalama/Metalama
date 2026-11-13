// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Workspaces.Navigation.Links;
using Metalama.Framework.Workspaces.Navigation.Results;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Metalama.Mcp.Rendering;

/// <summary>
/// Renders a result tree as an indented outline for the agent. Each object starts with its link, relative to the base.
/// </summary>
internal sealed class TextRenderer
{
    private const int _maxCellWidth = 60;
    private const int _maxTableColumns = 8;
    private const int _maxLinkColumnWidth = 80;

    private readonly LinkBase? _base;

    public TextRenderer( LinkBase? linkBase )
    {
        this._base = linkBase;
    }

    public string Render( ResultNode node )
    {
        var builder = new StringBuilder();
        this.RenderBlock( node, builder, "", isRoot: true );

        return builder.ToString().TrimEnd();
    }

    private void RenderBlock( ResultNode node, StringBuilder builder, string indent, bool isRoot = false )
    {
        switch ( node )
        {
            case ObjectNode { Properties: { } properties } obj:
                builder.Append( indent ).AppendLine( this.GetHeader( obj ) );

                var nameWidth = properties.Length == 0 ? 0 : Math.Min( 24, properties.Max( p => p.Name.Length ) );

                foreach ( var property in properties )
                {
                    if ( IsExpanded( property.Value ) )
                    {
                        builder.Append( indent ).Append( "  " ).AppendLine( property.Name );
                        this.RenderBlock( property.Value, builder, indent + "    " );
                    }
                    else
                    {
                        builder.Append( indent ).Append( "  " ).Append( property.Name.PadRight( nameWidth ) ).Append( "  " ).AppendLine( this.Inline( property.Value ) );
                    }
                }

                break;

            case CollectionNode { Items: { } items } collection:
                builder.Append( indent ).AppendLine( CultureInfo.InvariantCulture, $"{collection.TypeName} — {FormatCount( collection, items.Length )}" );
                var rowIndent = isRoot ? indent : indent + "  ";
                this.RenderRows( items, builder, rowIndent );

                if ( collection.IsTruncated )
                {
                    var shown = items.Length.ToString( CultureInfo.InvariantCulture );
                    var total = collection.Count?.ToString( CultureInfo.InvariantCulture ) ?? "?";
                    builder.Append( rowIndent ).AppendLine( CultureInfo.InvariantCulture, $"truncated: {shown} of {total} shown; append .Skip({shown}) or pass maxItems" );
                }

                break;

            case GroupingNode grouping:
                builder.Append( indent ).AppendLine( CultureInfo.InvariantCulture, $"{this.Inline( grouping.Key )} — {FormatCount( grouping.Items, grouping.Items.Items?.Length ?? 0 )}" );

                if ( grouping.Items.Items is { } groupItems )
                {
                    this.RenderRows( groupItems, builder, indent + "  " );
                }

                break;

            default:
                builder.Append( indent ).AppendLine( this.Inline( node ) );

                break;
        }
    }

    private void RenderRows( IReadOnlyList<ResultNode> items, StringBuilder builder, string indent )
    {
        if ( this.TryRenderTable( items, builder, indent ) )
        {
            return;
        }

        // Declarations and other linked objects are one line each: the link, then the location or the display text.
        var linkWidth = Math.Min(
            _maxLinkColumnWidth,
            items.OfType<ObjectNode>().Where( o => o.Properties == null && o.Link != null ).Select( o => this.FormatLink( o.Link! ).Length ).DefaultIfEmpty( 0 ).Max() );

        foreach ( var item in items )
        {
            if ( IsExpanded( item ) || item is GroupingNode )
            {
                this.RenderBlock( item, builder, indent );
            }
            else if ( item is ObjectNode { Link: { } link } obj )
            {
                var suffix = obj.IsDeclaration ? obj.Location : obj.Display;
                builder.Append( indent ).Append( this.FormatLink( link ).PadRight( linkWidth ) );

                if ( !string.IsNullOrEmpty( suffix ) )
                {
                    builder.Append( "  " ).Append( suffix );
                }

                builder.AppendLine();
            }
            else
            {
                builder.Append( indent ).AppendLine( this.Inline( item ) );
            }
        }
    }

    /// <summary>
    /// Renders items as an aligned table when they are expanded objects of the same type with few, inline properties,
    /// which is what a projection to an anonymous type produces.
    /// </summary>
    private bool TryRenderTable( IReadOnlyList<ResultNode> items, StringBuilder builder, string indent )
    {
        if ( items.Count == 0 || items.Any( i => i is not ObjectNode { Properties: not null, IsDeclaration: false } ) )
        {
            return false;
        }

        var objects = items.Cast<ObjectNode>().ToList();
        var columns = objects[0].Properties!.Value.Select( p => p.Name ).ToList();

        if ( columns.Count == 0 || columns.Count > _maxTableColumns
                                || objects.Any( o => o.TypeName != objects[0].TypeName || o.Properties!.Value.Any( p => IsExpanded( p.Value ) ) ) )
        {
            return false;
        }

        var cells = objects.Select( o => o.Properties!.Value.Select( p => Truncate( this.Inline( p.Value ), _maxCellWidth ) ).ToList() ).ToList();
        var widths = columns.Select( ( c, i ) => Math.Max( c.Length, cells.Max( r => r[i].Length ) ) ).ToList();

        builder.Append( indent ).AppendLine( string.Join( "  ", columns.Select( ( c, i ) => c.PadRight( widths[i] ) ) ).TrimEnd() );

        foreach ( var row in cells )
        {
            builder.Append( indent ).AppendLine( string.Join( "  ", row.Select( ( c, i ) => c.PadRight( widths[i] ) ) ).TrimEnd() );
        }

        return true;
    }

    /// <summary>
    /// Formats a value on one line.
    /// </summary>
    internal string Inline( ResultNode node )
        => node switch
        {
            ScalarNode { Kind: ScalarKind.Null } => "null",
            ScalarNode { Kind: ScalarKind.String, Text: "" } => "\"\"",
            ScalarNode { Kind: ScalarKind.String } scalar => scalar.Text!.Replace( "\r", "\\r", StringComparison.Ordinal ).Replace( "\n", "\\n", StringComparison.Ordinal ),
            ScalarNode scalar => scalar.Text ?? "",
            ObjectNode { IsDeclaration: true, Link: { } link } obj => this.FormatLink( link ) + (obj.IsTransformed ? " [transformed]" : ""),
            ObjectNode { IsDeclaration: true } obj => obj.Display + " ‹no link›",
            ObjectNode { Link: { } link } obj => $"{obj.Display}  → {this.FormatLink( link )}",
            ObjectNode obj => obj.Display,
            CollectionNode collection => FormatCollapsedCount( collection ) + (collection.Link != null ? "  → " + this.FormatLink( collection.Link ) : ""),
            GroupingNode grouping => $"{this.Inline( grouping.Key )} => {FormatCount( grouping.Items, 0 )}",
            ErrorNode error => $"‹error: {error.ExceptionType}: {error.Message}›",
            RichTextNode text => text.Text,
            _ => node.ToString() ?? ""
        };

    private string GetHeader( ObjectNode obj )
    {
        string header;

        if ( obj.IsDeclaration && obj.Link is { Steps.IsEmpty: true, IsIdentifierAnchor: true } link )
        {
            header = $"D<{obj.InterfaceName ?? "IDeclaration"}>(\"{link.Anchor}\"{this.GetScopeArguments( link )})";
        }
        else if ( obj.Link != null )
        {
            header = $"{obj.TypeName}  {obj.Display}  → {this.FormatLink( obj.Link )}";
        }
        else
        {
            header = obj.Display == obj.TypeName ? obj.TypeName : $"{obj.TypeName}  {obj.Display}";
        }

        return obj.Location != null ? $"{header}  {obj.Location}" : header;
    }

    private string GetScopeArguments( Link link )
    {
        var arguments = new StringBuilder();

        if ( this._base?.Scope != link.Scope && link.Scope != Link.WholeWorkspaceScope )
        {
            var at = link.Scope.LastIndexOf( '@' );

            arguments.Append( at < 0 ? $", project: \"{link.Scope}\"" : $", project: \"{link.Scope.Substring( 0, at )}\", tfm: \"{link.Scope.Substring( at + 1 )}\"" );
        }

        if ( link.IsTransformed )
        {
            arguments.Append( ", transformed: true" );
        }

        return arguments.ToString();
    }

    private string FormatLink( Link link ) => link.ToRelativeString( this._base );

    private static bool IsExpanded( ResultNode node ) => node is ObjectNode { Properties: not null } or CollectionNode { Items: not null } or GroupingNode;

    private static string FormatCount( CollectionNode collection, int shown )
    {
        var count = collection.Count;
        var text = count switch
        {
            null => "? items",
            1 => "1 item",
            _ => $"{count.Value.ToString( CultureInfo.InvariantCulture )} items"
        };

        return collection.IsTruncated ? $"{text}, showing {shown.ToString( CultureInfo.InvariantCulture )}" : text;
    }

    private static string FormatCollapsedCount( CollectionNode collection )
        => collection.Count switch
        {
            null => "[?]",
            0 => "[]",
            var count => $"[{count.Value.ToString( CultureInfo.InvariantCulture )}]"
        };

    private static string Truncate( string text, int width ) => text.Length <= width ? text : text.Substring( 0, width - 1 ) + "…";
}
