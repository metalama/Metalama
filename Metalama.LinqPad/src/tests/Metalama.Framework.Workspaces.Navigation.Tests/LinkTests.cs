// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Workspaces.Navigation.Links;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Workspaces.Navigation.Tests;

public sealed class LinkTests : UnitTestClass
{
    private const string _code = """
                                 namespace Ns
                                 {
                                     public class C<T>
                                     {
                                         public C( string s ) {}
                                         public int P { get; set; }
                                         public int this[int i] => i;
                                         public event System.EventHandler? E;
                                         public int F;
                                         public void M( int a, T b ) {}
                                         public U G<U>( System.Collections.Generic.List<U> l ) => default!;
                                         public static C<T> operator +( C<T> x, C<T> y ) => x;
                                         ~C() {}
                                         public class Nested {}
                                     }

                                     public enum E { A, B }

                                     public delegate void D( int x );
                                 }
                                 """;

    [Fact]
    public void EveryDeclarationKindRoundTrips()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );
        var context = new TestLinkContext( compilation );
        var builder = new LinkBuilder( context );
        var resolver = new LinkResolver( context );

        var declarations = new List<IDeclaration>();

        foreach ( var type in compilation.AllTypes )
        {
            declarations.Add( type );
            declarations.AddRange( type.TypeParameters );
            declarations.AddRange( type.Members() );

            if ( type.Finalizer != null )
            {
                declarations.Add( type.Finalizer );
            }

            foreach ( var method in type.Methods.Concat<IHasParameters>( type.Constructors ).Concat( type.Indexers ) )
            {
                declarations.AddRange( method.Parameters );
            }

            foreach ( var method in type.Methods )
            {
                declarations.AddRange( method.TypeParameters );
            }
        }

        Assert.Contains( declarations, d => d.DeclarationKind == DeclarationKind.Parameter );
        Assert.Contains( declarations, d => d.DeclarationKind == DeclarationKind.Method && d is IMethod { MethodKind: MethodKind.Operator } );
        Assert.Contains( declarations, d => d.DeclarationKind == DeclarationKind.Method && d is IMethod { MethodKind: MethodKind.Finalizer } );
        Assert.Contains( declarations, d => d.DeclarationKind == DeclarationKind.TypeParameter );

        foreach ( var declaration in declarations )
        {
            var link = builder.GetAnchorLink( declaration );
            Assert.NotNull( link );
            Assert.Empty( link.Steps );
            Assert.False( link.IsFragile );

            // Through the relative and the absolute text forms.
            foreach ( var text in new[] { link.ToRelativeString( TestLinkContext.Base ), link.ToAbsoluteString() } )
            {
                Assert.True( LinkParser.TryParse( text, TestLinkContext.Base, out var parsed, out var parseError ), parseError );
                Assert.True( resolver.TryResolve( parsed, out var resolved, out var resolveError ), $"{text}: {resolveError}" );
                Assert.Equal( declaration, resolved );
            }
        }
    }

    [Fact]
    public void PropertyAndIndexStepsResolve()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );
        var context = new TestLinkContext( compilation );
        var resolver = new LinkResolver( context );
        var type = compilation.Types.OfName( "C" ).Single();

        Assert.True( LinkParser.TryParse( "T:Ns.C`1/Methods/0/Parameters", TestLinkContext.Base, out var link, out var error ), error );
        Assert.True( link.IsFragile );
        Assert.True( resolver.TryResolve( link, out var value, out error ), error );
        Assert.Equal( type.Methods.First().Parameters, value );
    }

    [Fact]
    public void HiddenPropertyIsNotAStep()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );
        var resolver = new LinkResolver( new TestLinkContext( compilation ) );

        Assert.True( LinkParser.TryParse( "T:Ns.E/Depth", TestLinkContext.Base, out var link, out _ ) );
        Assert.False( resolver.TryResolve( link, out _, out var error ) );
        Assert.Contains( "no displayed property 'Depth'", error, StringComparison.Ordinal );
    }

    [Fact]
    public void KeyAndQueryStepsAreNotSupportedYet()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );
        var resolver = new LinkResolver( new TestLinkContext( compilation ) );

        Assert.True( LinkParser.TryParse( "T:Ns.E/aspects/X", TestLinkContext.Base, out var keyLink, out _ ) );
        Assert.False( resolver.TryResolve( keyLink, out _, out var keyError ) );
        Assert.Contains( "not supported yet", keyError, StringComparison.Ordinal );

        Assert.True( LinkParser.TryParse( "metalama://test/~/source/query/abc/1", null, out var queryLink, out _ ) );
        Assert.True( queryLink.IsQueryAnchor );
        Assert.False( resolver.TryResolve( queryLink, out _, out var queryError ) );
        Assert.Contains( "not supported yet", queryError, StringComparison.Ordinal );
    }

    [Fact]
    public void FragileLinkIsStaleAfterChange()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( _code );
        var context = new TestLinkContext( compilation );
        var resolver = new LinkResolver( context );

        var link = new LinkBuilder( context ).GetAnchorLink( compilation.Types.OfName( "C" ).Single() )!.WithStep( "Methods" ).WithStep( "0" );
        var text = link.ToRelativeString( TestLinkContext.Base );
        Assert.EndsWith( "?v=0123456789ab", text, StringComparison.Ordinal );

        Assert.True( LinkParser.TryParse( text, TestLinkContext.Base, out var parsed, out _ ) );
        Assert.True( resolver.TryResolve( parsed, out _, out _ ) );

        context.Fingerprint = "ffffffffffff";
        Assert.False( resolver.TryResolve( parsed, out _, out var error ) );
        Assert.StartsWith( "stale:", error, StringComparison.Ordinal );

        // A stable link carries no fingerprint and survives the change.
        Assert.True( LinkParser.TryParse( "T:Ns.C`1/Methods", TestLinkContext.Base, out var stable, out _ ) );
        Assert.True( resolver.TryResolve( stable, out _, out error ), error );
    }

    [Theory]
    [InlineData( "M:Ns.C`1.#ctor(System.String)" )]
    [InlineData( "M:Ns.C%601.%23ctor(System.String)" )]
    public void ParserDecodesLeniently( string text )
    {
        Assert.True( LinkParser.TryParse( text, TestLinkContext.Base, out var link, out var error ), error );
        Assert.Equal( "M:Ns.C`1.#ctor(System.String)", link.Anchor );
        Assert.Equal( TestLinkContext.Scope, link.Scope );
    }

    [Fact]
    public void AbsoluteFormIsEncodedAndRelativeFormIsRaw()
    {
        var link = new Link( "src/App.sln", "App@net10.0", false, "M:Ns.C`1.#ctor(System.String)", [], null );

        Assert.Equal( "metalama://src%2FApp.sln/App@net10.0/source/M:Ns.C%601.%23ctor(System.String)", link.ToAbsoluteString() );
        Assert.Equal( "M:Ns.C`1.#ctor(System.String)", link.ToRelativeString( new LinkBase( "src/App.sln", "App@net10.0" ) ) );
        Assert.Equal( "App@net10.0/source/M:Ns.C`1.#ctor(System.String)", link.ToRelativeString( new LinkBase( "src/App.sln", null ) ) );
        Assert.Equal( link.ToAbsoluteString(), link.ToRelativeString( new LinkBase( "other.sln", null ) ) );

        Assert.True( LinkParser.TryParse( link.ToAbsoluteString(), null, out var parsed, out _ ) );
        Assert.Equal( link, parsed with { Fingerprint = null } );
    }

    [Fact]
    public void ScopeIsRequiredWhenTheBaseHasNone()
    {
        Assert.False( LinkParser.TryParse( "T:Ns.E", new LinkBase( "ws", null ), out _, out var error ) );
        Assert.Contains( "has no scope", error, StringComparison.Ordinal );

        Assert.True( LinkParser.TryParse( "App@net48/transformed/T:Ns.E", new LinkBase( "ws", null ), out var link, out error ), error );
        Assert.Equal( "App@net48", link.Scope );
        Assert.True( link.IsTransformed );
    }
}
