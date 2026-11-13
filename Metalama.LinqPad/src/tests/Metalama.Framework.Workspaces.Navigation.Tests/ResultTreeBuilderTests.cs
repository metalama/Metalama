// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Workspaces.Navigation.Links;
using Metalama.Framework.Workspaces.Navigation.Results;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Workspaces.Navigation.Tests;

public sealed class ResultTreeBuilderTests : UnitTestClass
{
    private static ResultNode Build( object? value, int maxDepth = 1, int maxItems = 50 ) => new ResultTreeBuilder( new ResultTreeOptions( maxDepth, maxItems ) ).Build( value );

    [Fact]
    public void Scalars()
    {
        Assert.Equal( ScalarKind.Null, Assert.IsType<ScalarNode>( Build( null ) ).Kind );
        Assert.Equal( ScalarKind.String, Assert.IsType<ScalarNode>( Build( "s" ) ).Kind );
        Assert.Equal( "42", Assert.IsType<ScalarNode>( Build( 42 ) ).Text );
        Assert.Equal( "true", Assert.IsType<ScalarNode>( Build( true ) ).Text );
        Assert.Equal( ScalarKind.Enum, Assert.IsType<ScalarNode>( Build( Accessibility.Internal ) ).Kind );
        Assert.Equal( ScalarKind.Other, Assert.IsType<ScalarNode>( Build( Guid.Empty ) ).Kind );

        var severity = Assert.IsType<ScalarNode>( Build( Severity.Warning ) );
        Assert.Equal( ScalarKind.Severity, severity.Kind );
        Assert.Equal( "Warning", severity.Text );
    }

    [Fact]
    public void CollectionsAreCountedWithoutEnumeration()
    {
        var root = Assert.IsType<ObjectNode>( Build( new { List = new List<int> { 1, 2 }, Lazy = Enumerable.Range( 0, 3 ).Select( i => i ), Empty = Array.Empty<int>() } ) );

        var list = Assert.IsType<CollectionNode>( root.Properties!.Value.Single( p => p.Name == "List" ).Value );
        Assert.Equal( 2, list.Count );
        Assert.Null( list.Items );

        var lazy = Assert.IsType<CollectionNode>( root.Properties!.Value.Single( p => p.Name == "Lazy" ).Value );
        Assert.Null( lazy.Count );
        Assert.Null( lazy.Items );

        var empty = Assert.IsType<CollectionNode>( root.Properties!.Value.Single( p => p.Name == "Empty" ).Value );
        Assert.Equal( 0, empty.Count );
    }

    [Fact]
    public void RootCollectionIsTruncated()
    {
        var collection = Assert.IsType<CollectionNode>( Build( Enumerable.Range( 0, 10 ).Select( i => i ), maxItems: 3 ) );

        Assert.Equal( 3, collection.Items!.Value.Length );
        Assert.True( collection.IsTruncated );
        Assert.Null( collection.Count );
    }

    [Fact]
    public void ProjectionRowsAreExpanded()
    {
        var collection = Assert.IsType<CollectionNode>( Build( new[] { new { Name = "a", Complexity = 1 }, new { Name = "b", Complexity = 2 } } ) );

        Assert.Equal( 2, collection.Count );
        var row = Assert.IsType<ObjectNode>( collection.Items!.Value[0] );
        Assert.Equal( "{ Name, Complexity }", row.TypeName );
        Assert.Equal( ["Name", "Complexity"], row.Properties!.Value.Select( p => p.Name ).ToArray() );
    }

    [Fact]
    public void Groupings()
    {
        var collection = Assert.IsType<CollectionNode>( Build( new[] { (1, 2), (1, 3), (2, 4) }.GroupBy( t => t.Item1 ) ) );
        var group = Assert.IsType<GroupingNode>( collection.Items!.Value[0] );

        Assert.Equal( "1", Assert.IsType<ScalarNode>( group.Key ).Text );
        Assert.Equal( 2, group.Items.Count );
    }

    [Fact]
    public void ThrowingGetterBecomesErrorNode()
    {
        var root = Assert.IsType<ObjectNode>( Build( new Throwing() ) );
        var error = Assert.IsType<ErrorNode>( root.Properties!.Value.Single( p => p.Name == nameof(Throwing.Value) ).Value );

        Assert.Equal( nameof(InvalidOperationException), error.ExceptionType );
        Assert.Equal( "boom", error.Message );
    }

    [Fact]
    public void DeclarationsHaveLinksAndLocations()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( "class C { void M() {} int P => 0; }" );
        var builder = new ResultTreeBuilder( new ResultTreeOptions(), new LinkBuilder( new TestLinkContext( compilation ) ) );
        var type = compilation.Types.Single();

        var root = Assert.IsType<ObjectNode>( builder.Build( type ) );
        Assert.True( root.IsDeclaration );
        Assert.Equal( "T:C", root.Link!.Anchor );
        Assert.Equal( nameof(INamedType), root.InterfaceName );
        Assert.NotNull( root.Location );

        // Properties of a declaration are reached by property steps, and declarations collapse to their own identifier.
        var methods = Assert.IsType<CollectionNode>( root.Properties!.Value.Single( p => p.Name == nameof(INamedType.Methods) ).Value );
        Assert.Equal( ["Methods"], methods.Link!.Steps );

        var list = Assert.IsType<CollectionNode>( builder.Build( type.Methods ) );
        var method = Assert.IsType<ObjectNode>( list.Items!.Value.Single() );
        Assert.Equal( "M:C.M", method.Link!.Anchor );
        Assert.Null( method.Properties );
    }

    [Fact]
    public void FormattableStringKeepsObjectArguments()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( "class C {}" );
        var type = compilation.Types.Single();
        var builder = new ResultTreeBuilder( new ResultTreeOptions(), new LinkBuilder( new TestLinkContext( compilation ) ) );

        var node = Assert.IsType<RichTextNode>( builder.Build( (FormattableString) $"The type {type} has {0} members." ) );

        Assert.Contains( "has 0 members.", node.Text, StringComparison.Ordinal );
        var argument = Assert.Single( node.Segments, s => s.Argument != null ).Argument;
        Assert.Equal( "T:C", argument!.Link!.Anchor );
    }

    private sealed class Throwing
    {
        private readonly string _message = "boom";

        public int Value => throw new InvalidOperationException( this._message );
    }
}
