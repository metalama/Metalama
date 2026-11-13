// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Mcp.Tests;

/// <summary>
/// Drives the server over stdio with the FrogHattery sample loaded.
/// </summary>
public sealed class WorkspaceEndToEndTests : IClassFixture<FrogHatteryFixture>
{
    private readonly FrogHatteryFixture _fixture;

    public WorkspaceEndToEndTests( FrogHatteryFixture fixture )
    {
        this._fixture = fixture;
    }

    private ServerProcess Server => this._fixture.Server;

    [Fact]
    public void LoadReportsProjectsAndBase()
    {
        Assert.Contains( "workspace: FrogHattery.csproj", this._fixture.LoadText, StringComparison.Ordinal );
        Assert.Contains( "FrogHattery@net10.0", this._fixture.LoadText, StringComparison.Ordinal );
        Assert.Contains( "base: metalama://FrogHattery.csproj/FrogHattery@net10.0/source/", this._fixture.LoadText, StringComparison.Ordinal );
        Assert.Contains( "Metalama enabled", this._fixture.LoadText, StringComparison.Ordinal );
    }

    [Fact]
    public async Task QueryListsDeclarationsWithLinksAndLocations()
    {
        var text = await this.Server.QueryAsync( "workspace.SourceCode.Types.Where( t => t.Name.EndsWith( \"Frog\" ) ).OrderBy( t => t.Name )" );

        Assert.Contains( "T:FrogHattery.BullFrog", text, StringComparison.Ordinal );
        Assert.Contains( "BullFrog.cs:", text, StringComparison.Ordinal );
        Assert.Contains( "fingerprint ", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task EveryLinkInTheOutputOpens()
    {
        var text = await this.Server.QueryAsync( "D<INamedType>( \"T:FrogHattery.BullFrog\" )" );
        Assert.Contains( "D<INamedType>(\"T:FrogHattery.BullFrog\")", text, StringComparison.Ordinal );

        // Every value with a link is printed as the link itself or after an arrow.
        var links = text.Split( '\n' )
            .Select( l => l.Contains( "→ ", StringComparison.Ordinal ) ? l.Substring( l.IndexOf( "→ ", StringComparison.Ordinal ) + 2 ).Trim() : l.Trim().Split( "  " ).Last() )
            .Where( l => l.StartsWith( "T:", StringComparison.Ordinal ) || l.StartsWith( "M:", StringComparison.Ordinal ) || l.StartsWith( "P:", StringComparison.Ordinal ) )
            .Distinct()
            .ToList();

        Assert.NotEmpty( links );

        foreach ( var link in links )
        {
            var (isError, opened, _) = await this.Server.CallAsync( "open", new Dictionary<string, object?> { ["link"] = link } );
            Assert.False( isError, $"{link}: {opened}" );
        }
    }

    [Fact]
    public async Task OpenFollowsPropertySteps()
    {
        var text = await this.Server.CallSuccessfullyAsync( "open", new Dictionary<string, object?> { ["link"] = "T:FrogHattery.BullFrog/Methods" } );

        Assert.Contains( "M:FrogHattery.BullFrog.", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task OpenReportsHiddenPropertiesAndUnknownDeclarations()
    {
        var (isError, text, _) = await this.Server.CallAsync( "open", new Dictionary<string, object?> { ["link"] = "T:FrogHattery.BullFrog/Depth" } );
        Assert.True( isError );
        Assert.Contains( "no displayed property 'Depth'", text, StringComparison.Ordinal );

        (isError, text, _) = await this.Server.CallAsync( "open", new Dictionary<string, object?> { ["link"] = "T:FrogHattery.Nope" } );
        Assert.True( isError );
        Assert.Contains( "not found", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task IndexStepsCarryTheFingerprint()
    {
        var text = await this.Server.CallSuccessfullyAsync( "open", new Dictionary<string, object?> { ["link"] = "T:FrogHattery.BullFrog/Methods" } );

        // Items of a collection reached by a link get index links only when they have no identifier of their own, so open
        // one through an index explicitly.
        Assert.DoesNotContain( "error", text.Split( '\n' )[0], StringComparison.Ordinal );

        var opened = await this.Server.CallSuccessfullyAsync( "open", new Dictionary<string, object?> { ["link"] = "T:FrogHattery.BullFrog/Methods/0/Parameters" } );
        Assert.Contains( "IParameter", opened, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ProjectionIsATable()
    {
        var text = await this.Server.QueryAsync( "workspace.SourceCode.Types.OrderBy( t => t.Name ).Select( t => new { t.Name, Methods = t.Methods.Count } )" );

        Assert.Contains( "Name", text.Split( '\n' ).First( l => l.StartsWith( "Name", StringComparison.Ordinal ) ), StringComparison.Ordinal );
        Assert.Contains( "Methods", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task StatementsAndDeclarationHelpers()
    {
        var text = await this.Server.QueryAsync( "var type = D<INamedType>( \"T:FrogHattery.BullFrog\" );\nreturn type.Name.Length;" );

        Assert.StartsWith( "base: ", text, StringComparison.Ordinal );
        Assert.Contains( "\n8\n", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task CompileErrorsPointAtTheQuery()
    {
        var (isError, text, _) = await this.Server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "workspace.Nope" } );

        Assert.True( isError );
        Assert.Contains( "CS1061 (1,11)", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ExceptionsAreReported()
    {
        var (isError, text, _) = await this.Server.CallAsync( "query", new Dictionary<string, object?> { ["code"] = "D( \"T:FrogHattery.Nope\" )" } );

        Assert.True( isError );
        Assert.Contains( "not found", text, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ConsoleOutputIsCapturedAndTheStreamStaysValid()
    {
        var text = await this.Server.QueryAsync( "Console.WriteLine( \"hello from the query\" );\nreturn 1;" );

        Assert.Contains( "--- console ---\nhello from the query", text, StringComparison.Ordinal );

        // The MCP stream is intact.
        Assert.Contains( "\n2\n", await this.Server.QueryAsync( "1 + 1" ), StringComparison.Ordinal );
    }

    [Fact]
    public async Task HtmlFormatWritesASelfContainedFile()
    {
        var text = await this.Server.QueryAsync( "D<INamedType>( \"T:FrogHattery.BullFrog\" )", "html" );

        var line = text.Split( '\n' ).Single( l => l.StartsWith( "html: ", StringComparison.Ordinal ) );
        var path = new Uri( line.Substring( "html: ".Length ).Trim() ).LocalPath;
        Assert.True( File.Exists( path ), path );

        var html = await File.ReadAllTextAsync( path, TestContext.Current.CancellationToken );
        Assert.Contains( "Content-Security-Policy", html, StringComparison.Ordinal );
        Assert.Contains( "id=\"metalama-data\"", html, StringComparison.Ordinal );
        Assert.Contains( "T:FrogHattery.BullFrog", html, StringComparison.Ordinal );
        Assert.DoesNotContain( "http://", html, StringComparison.Ordinal );
        Assert.DoesNotContain( "https://", html, StringComparison.Ordinal );
    }

    [Fact]
    public async Task JsonFormatFollowsTheSchema()
    {
        var text = await this.Server.QueryAsync( "D<INamedType>( \"T:FrogHattery.BullFrog\" )", "json" );

        var json = text.Substring( 0, text.LastIndexOf( "\n--- ", StringComparison.Ordinal ) );
        using var document = System.Text.Json.JsonDocument.Parse( json );
        var root = document.RootElement;

        Assert.Equal( 1, root.GetProperty( "schema" ).GetInt32() );
        var result = root.GetProperty( "result" );
        Assert.Equal( "object", result.GetProperty( "kind" ).GetString() );
        Assert.True( result.GetProperty( "declaration" ).GetBoolean() );
        Assert.Equal( "T:FrogHattery.BullFrog", result.GetProperty( "link" ).GetString() );
        Assert.StartsWith( "metalama://", result.GetProperty( "href" ).GetString(), StringComparison.Ordinal );
        Assert.True( result.GetProperty( "properties" ).GetArrayLength() > 0 );
    }

    [Fact]
    public async Task EnumerationOrderIsStableWithinALoad()
    {
        const string query = "string.Join( \",\", workspace.SourceCode.Methods.Select( m => m.ToSerializableId().Id ) )";

        Assert.Equal( Body( await this.Server.QueryAsync( query ) ), Body( await this.Server.QueryAsync( query ) ) );
    }

    [Fact]
    public async Task IndexLinksAreStaleAfterAReloadAndDeclarationLinksAreNot()
    {
        // The order of collections such as SourceCode.Methods can differ between two loads of identical sources, so an
        // index link is valid only for the load that produced it.
        var fingerprint = GetFingerprint( await this.Server.QueryAsync( "0" ) );
        var indexLink = $"T:FrogHattery.BullFrog/Methods/0?v={fingerprint}";
        await this.Server.CallSuccessfullyAsync( "open", new Dictionary<string, object?> { ["link"] = indexLink } );

        await this.Server.CallSuccessfullyAsync( "reload_workspace", new Dictionary<string, object?>() );

        var (isError, text, _) = await this.Server.CallAsync( "open", new Dictionary<string, object?> { ["link"] = indexLink } );
        Assert.True( isError );
        Assert.Contains( "stale:", text, StringComparison.Ordinal );

        await this.Server.CallSuccessfullyAsync( "open", new Dictionary<string, object?> { ["link"] = "T:FrogHattery.BullFrog/Methods" } );
        Assert.NotEqual( fingerprint, GetFingerprint( await this.Server.QueryAsync( "0" ) ) );
    }

    private static string Body( string text ) => text.Split( '\n' )[1];

    private static string GetFingerprint( string text )
    {
        var footer = text.Split( '\n' ).Last();
        var start = footer.IndexOf( "fingerprint ", StringComparison.Ordinal ) + "fingerprint ".Length;

        return footer.Substring( start, 12 );
    }
}
