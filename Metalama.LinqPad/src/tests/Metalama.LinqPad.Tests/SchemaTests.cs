// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using LINQPad.Extensibility.DataContext;
using Metalama.Framework.Workspaces;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.LinqPad.Tests;

#pragma warning disable VSTHRD200

public sealed class SchemaTests : UnitTestClass
{
    private readonly ITestOutputHelper _logger;

    static SchemaTests()
    {
        Initializer.Initialize();
    }

    public SchemaTests( ITestOutputHelper logger )
    {
        this._logger = logger;
    }

    [Fact]
    public void SchemaWithoutWorkspace()
    {
        var factory = new SchemaFactory( ( type, _ ) => type.ToString() );

        var schema = factory.GetSchema( "workspace" );

        var xml = new XDocument();
        xml.Add( new XElement( "schema", schema.Select( item => (object) ConvertToXml( item ) ) ) );

        var xmlString = xml.ToString();
        this._logger.WriteLine( xmlString );
    }

    [Fact( Skip = "Cannot get MSBuildLocator to work." )]
    public async Task SchemaWithWorkspace()
    {
        using var testContext = this.CreateTestContext();

        var projectPath = Path.Combine( testContext.BaseDirectory, "Project.csproj" );
        var codePath = Path.Combine( testContext.BaseDirectory, "Code.cs" );

        await File.WriteAllTextAsync(
            projectPath,
            @"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>
</Project>
" );

        await File.WriteAllTextAsync( codePath, "class MyClass {}" );

        var workspaceCollection = new WorkspaceCollection();

        using var workspace = await workspaceCollection.LoadAsync( projectPath );

        var factory = new SchemaFactory( ( type, _ ) => type.ToString() );

        var schema = factory.GetSchema( "workspace", workspace );
        var xml = new XDocument();
        xml.Add( new XElement( "schema", schema.Select( item => (object) ConvertToXml( item ) ) ) );
        var xmlString = xml.ToString();
        this._logger.WriteLine( xmlString );
    }

    [Fact]
    public async Task ProjectsShouldBeSortedByName()
    {
        var solutionPath = Path.GetFullPath(
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "MultiProjectSolution", "MultiProjectSolution.sln" ) );

        Assert.True( File.Exists( solutionPath ), $"Solution file not found at: {solutionPath}" );

        var workspaceCollection = new WorkspaceCollection();

        using var workspace = await workspaceCollection.LoadAsync( ImmutableArray.Create( solutionPath ), restore: false );

        var factory = new SchemaFactory( ( type, _ ) => type.ToString() );

        var schema = factory.GetSchema( "workspace", workspace );

        // Find the "Projects" node which contains individual project items.
        var projectsItem = schema.FirstOrDefault( item => item.Text == "Projects" );
        Assert.NotNull( projectsItem );
        Assert.NotNull( projectsItem.Children );
        Assert.True( projectsItem.Children.Count >= 3, $"Expected at least 3 projects, found {projectsItem.Children.Count}" );

        var projectNames = projectsItem.Children.Select( c => c.Text ).ToList();

        this._logger.WriteLine( "Project order: " + string.Join( ", ", projectNames ) );

        // Verify projects are sorted alphabetically by name.
        var sortedNames = projectNames.OrderBy( n => n, StringComparer.OrdinalIgnoreCase ).ToList();
        Assert.Equal( sortedNames, projectNames );
    }

    private static XElement ConvertToXml( ExplorerItem item )
    {
        var element = new XElement(
            "item",
            new XAttribute( "text", item.Text ),
            new XAttribute( "dragText", item.DragText ?? "" ),
            new XAttribute( "tooltip", item.ToolTipText ?? "" ),
            new XAttribute( "isEnumerable", item.IsEnumerable ) );

        if ( item.Children != null )
        {
            element.Add( item.Children.Select( explorerItem => (object) ConvertToXml( explorerItem ) ) );
        }

        return element;
    }
}