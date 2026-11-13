// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Workspaces.Navigation.Links;
using Metalama.Framework.Workspaces.Navigation.Results;
using Metalama.Mcp.Rendering;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Metalama.Mcp.Tests;

public sealed class RenderingTests
{
    private static readonly LinkBase _base = new( "App.sln", "App@net10.0" );

    private static ResultNode Build( object? value, int maxDepth = 1, int maxItems = 50 ) => new ResultTreeBuilder( new ResultTreeOptions( maxDepth, maxItems ) ).Build( value );

    [Fact]
    public void ProjectionIsATable()
    {
        var text = new TextRenderer( _base ).Render( Build( new[] { new { Name = "Frog.Jump", Complexity = 12 }, new { Name = "BullFrog.Croak", Complexity = 9 } } ) );

        Assert.Equal(
            """
            { Name, Complexity }[] — 2 items
            Name            Complexity
            Frog.Jump       12
            BullFrog.Croak  9
            """.Replace( "\r\n", "\n", StringComparison.Ordinal ),
            text.Replace( "\r\n", "\n", StringComparison.Ordinal ) );
    }

    [Fact]
    public void TruncationIsReported()
    {
        var text = new TextRenderer( _base ).Render( Build( Enumerable.Range( 0, 10 ).Select( i => i ), maxItems: 3 ) );

        Assert.Contains( "IEnumerable<Int32> — ? items, showing 3", text, StringComparison.Ordinal );
        Assert.Contains( "truncated: 3 of ? shown; append .Skip(3) or pass maxItems", text, StringComparison.Ordinal );
    }

    [Fact]
    public void CollapsedValues()
    {
        var text = new TextRenderer( _base ).Render( Build( new { Lazy = Enumerable.Range( 0, 3 ).Select( i => i ), Empty = Array.Empty<int>(), Null = (string?) null, Text = "" } ) );

        Assert.Contains( "Lazy   [?]", text, StringComparison.Ordinal );
        Assert.Contains( "Empty  []", text, StringComparison.Ordinal );
        Assert.Contains( "Null   null", text, StringComparison.Ordinal );
        Assert.Contains( "Text   \"\"", text, StringComparison.Ordinal );
    }

    [Fact]
    public void JsonHasKindsAndLinks()
    {
        var node = new ObjectNode( "INamedType", "BullFrog" )
        {
            IsDeclaration = true, InterfaceName = "INamedType", Link = new Link( "App.sln", "App@net10.0", false, "T:App.BullFrog", [], "abc" ), Properties = []
        };

        var json = new JsonRenderer( _base ).RenderDocument( node );

        Assert.Equal( 1, json["schema"]!.GetValue<int>() );
        Assert.Equal( "T:App.BullFrog", json["result"]!["link"]!.GetValue<string>() );
        Assert.Equal( "metalama://App.sln/App@net10.0/source/T:App.BullFrog", json["result"]!["href"]!.GetValue<string>() );
        Assert.Equal( "D<INamedType>(\"T:App.BullFrog\")", json["result"]!["expression"]!.GetValue<string>() );
    }

    [Fact]
    public void HtmlCannotBeInjectedThroughValues()
    {
        const string hostile = "</script><img src=x onerror=alert(1)><script>alert(2)</script>{{SCRIPT}}";
        var json = new JsonRenderer( _base ).RenderDocument( Build( new[] { new { Name = hostile, Comment = "<!-- -->" } } ), hostile );

        var html = HtmlWriter.CreatePage( json, hostile );

        // Only the data element and the viewer script: the values are escaped inside the JSON, and the title is encoded.
        Assert.Equal( 2, Regex.Matches( html, "<script", RegexOptions.IgnoreCase ).Count );
        Assert.DoesNotContain( "<img", html, StringComparison.OrdinalIgnoreCase );
        Assert.DoesNotContain( "<!-- -->", html, StringComparison.Ordinal );
        Assert.Contains( "\\u003C/script\\u003E", html, StringComparison.Ordinal );

        // The placeholder in the data was not substituted.
        Assert.Single( Regex.Matches( html, "use strict" ) );
    }

    [Fact]
    public void HtmlHasAStrictContentSecurityPolicy()
    {
        var html = HtmlWriter.CreatePage( null, "t" );

        Assert.Matches( "default-src &#39;none&#39;; script-src &#39;sha256-[A-Za-z0-9+/=]+&#39;", html );
        Assert.DoesNotContain( "unsafe-eval", html, StringComparison.Ordinal );
        Assert.DoesNotContain( "id=\"metalama-data\"", html, StringComparison.Ordinal );
    }
}
