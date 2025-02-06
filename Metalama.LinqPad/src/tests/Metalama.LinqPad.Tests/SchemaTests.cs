// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using LINQPad.Extensibility.DataContext;
using Metalama.Framework.Workspaces;
using Metalama.Testing.UnitTesting;
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