// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.CodeFixes;
using Metalama.Framework.Engine.DesignTime;
using Metalama.Framework.Engine.DesignTime.CodeFixes;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.DevEnvEntryPoint;

public sealed class SerializationTests
{
    private static T Roundloop<T>( T input )
    {
        var stringWriter = new StringWriter();
        var serializer = JsonSerializer.Create( new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All } );
        serializer.Serialize( stringWriter, input );

        return serializer.Deserialize<T>( new JsonTextReader( new StringReader( stringWriter.ToString() ) ) )!;
    }

    [Fact]
    public void Serialize_AddAspectAttributeCodeActionModel()
    {
        var roundloop = (AddAspectAttributeCodeActionModel) Roundloop(
            (CodeActionBaseModel)
            new AddAspectAttributeCodeActionModel( "AspectTypeName", new SymbolId( "SymbolId" ), "SyntaxTreeFilePath", "Title" ) );

        Assert.Equal( "AspectTypeName", roundloop.AspectTypeName );
        Assert.Equal( "SymbolId", roundloop.TargetSymbolId.Id );
        Assert.Equal( "SyntaxTreeFilePath", roundloop.SyntaxTreeFilePath );
    }

    [Fact]
    public void Serialize_ImmutableArray()
    {
        var a = Roundloop( ImmutableArray.Create( 1, 2, 3 ) );
        Assert.Equal( 3, a.Length );
        Assert.Equal( 1, a[0] );
    }

    [Fact]
    public void Serialize_ComputeRefactoringResult()
    {
        var result = new ComputeRefactoringResult(
            ImmutableArray.Create<CodeActionBaseModel>(
                new AddAspectAttributeCodeActionModel( "AspectTypeName", new SymbolId( "SymbolId" ), "SyntaxTreeFilePath", "Title" ) ) );

        Roundloop( result );
    }

    [Fact]
    public void Serialize_CodeActionMenu()
    {
        var input = new CodeActionMenuModel( "The title" );
        input.Items.Add( new AddAspectAttributeCodeActionModel( "AspectTypeName", new SymbolId( "SymbolId" ), "SyntaxTreeFilePath", "Title" ) );

        var roundloop = (CodeActionMenuModel) Roundloop( (CodeActionBaseModel) input );

        Assert.Equal( "The title", roundloop.Title );
        Assert.Single( roundloop.Items );
    }

    [Fact]
    public void Serialize_CodeActionResult()
    {
        const string code = "class Program { static void Main() {} }";

        var input = CodeActionResult.Success(
            new[] { CSharpSyntaxTree.ParseText( code, path: "path.cs", options: SupportedCSharpVersions.DefaultParseOptions ) } );

        var roundloop = Roundloop( input );
        Assert.Single( roundloop.SyntaxTreeChanges );
        Assert.Equal( "path.cs", roundloop.SyntaxTreeChanges[0].FilePath );
        AssertEx.EolInvariantEqual( code, roundloop.SyntaxTreeChanges[0].Text );
    }

    [Fact]
    public void Serialize_SyntaxTree()
    {
        const string code = "class Program { static void Main() {} }";
        var tree = CSharpSyntaxTree.ParseText( code, path: "path.cs", options: SupportedCSharpVersions.DefaultParseOptions );
        var root = tree.GetRoot();
        var node = root.DescendantNodes().Single( n => n.IsKind( SyntaxKind.ClassDeclaration ) );
        var rootWithAnnotation = root.ReplaceNode( node, node.WithAdditionalAnnotations( Formatter.Annotation ) );
        var treeWithAnnotation = tree.WithRootAndOptions( rootWithAnnotation, tree.Options );
        var input = JsonSerializationHelper.CreateSerializableSyntaxTree( treeWithAnnotation );
        var roundloop = Roundloop( input );
        Assert.Single( roundloop.Annotations );
        Assert.Equal( SerializableAnnotationKind.Formatter, roundloop.Annotations[0].Kind );
        Assert.Equal( node.Span, new TextSpan( roundloop.Annotations[0].SpanStart, roundloop.Annotations[0].SpanLength ) );

        var roundloopRoot = roundloop.ToSyntaxNode();
        var roundloopNode = roundloopRoot.DescendantNodes().Single( n => n.IsKind( SyntaxKind.ClassDeclaration ) );
        Assert.True( roundloopNode.HasAnnotation( Formatter.Annotation ) );
    }
}