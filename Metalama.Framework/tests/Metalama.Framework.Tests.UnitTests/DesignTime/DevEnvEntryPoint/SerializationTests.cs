// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.DesignTime;
using Metalama.Framework.Engine.DesignTime.CodeFixes;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.DevEnvEntryPoint;

public sealed class SerializationTests : SerializationTestsBase
{
    [Fact]
    public void Serialize_ImmutableArray()
    {
        var a = Roundloop( ImmutableArray.Create( 1, 2, 3 ) );
        Assert.Equal( 3, a.Length );
        Assert.Equal( 1, a[0] );
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