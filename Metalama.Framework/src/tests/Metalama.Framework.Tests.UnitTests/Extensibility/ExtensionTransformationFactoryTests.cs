// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Extensibility.Transformations;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Services;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Extensibility;

/// <summary>
/// Tests of the validation that <see cref="ExtensionTransformationFactory"/> performs, and of <see cref="ExtensionTemplateServices"/>.
/// </summary>
/// <remarks>
/// Each test runs the compile-time pipeline with an extension whose transforming hook runs a script given by the test. The script receives the
/// factory of the stage and an origin that represents the aspect applied to the type <c>C</c>. The rewritten code is tested by the aspect tests
/// of the proof of concept in <c>Metalama.Framework.Tests.AspectTests.ExtensionPoints</c>.
/// </remarks>
public sealed class ExtensionTransformationFactoryTests : UnitTestClass
{
    private const string _code = """
                                 using Metalama.Framework.Aspects;
                                 using System;

                                 internal class TheAspect : TypeAspect
                                 {
                                     [Template]
                                     public void Template() { }

                                     public void NotTemplate() { }
                                 }

                                 internal static class Source
                                 {
                                     public static int Compute( int x ) => x;

                                     public static int Two( int a, int b ) => a;

                                     public static void Ref( ref int x ) { }

                                     public static void Out( int a, out int x ) => x = a;

                                     public static void Log( string message, string origin = "source" ) { }

                                     public static void Widen( long value ) { }
                                 }

                                 internal class Instance
                                 {
                                     public int Field;

                                     public int Get( int x ) => x;
                                 }

                                 internal static class Interceptors
                                 {
                                     public static int Compute( int x ) => x;

                                     public static int Two( int a, int b ) => a;

                                     public static int Get( Instance instance, int x ) => x;

                                     public static int GetExtension( this Instance instance, int x ) => x;

                                     public static void Ref( int x ) { }

                                     public static void Out( int a ) { }

                                     public static int None() => 0;

                                     public static void Widen( object value ) { }
                                 }

                                 internal class NonStatic
                                 {
                                     public int Compute( int x ) => x;
                                 }

                                 [TheAspect]
                                 internal class C
                                 {
                                     private const int Constant = 1;

                                     private int _field;

                                     private static int Property => 0;

                                     private void M( Instance i, Instance? n )
                                     {
                                         Source.Two( 10, Property );
                                         Source.Two( 11, Constant );
                                         Source.Two( 12, i.Field );
                                         Source.Two( 13, _field );
                                         Source /* callee */ . Compute( 14 );
                                         Source.Two( /* first */ 15, // second
                                             16 );
                                         Source.
                                 #if DEBUG
                                             Compute
                                 #else
                                             Compute
                                 #endif
                                             ( 17 );
                                         Source.Two( 18,
                                 #if DEBUG
                                             19
                                 #else
                                             20
                                 #endif
                                             );
                                         Func<int, int> g = Source /* group */ . Compute;
                                         Func<int, int> h = Source.
                                 #if DEBUG
                                             Compute;
                                 #else
                                             Compute;
                                 #endif
                                         Source.Compute( 1 );
                                         i.Get( 2 );
                                         n?.Get( 3 );
                                         var y = 0;
                                         Source.Ref( ref y );
                                         Source.Out( 4, out var z );
                                         Source.Two( 5, 6 );
                                         Source.Log( "message" );
                                         Func<int, int> f = Source.Compute;
                                         _ = nameof(Source.Two);
                                         int? r = n?.Get( 7 );
                                         Source.Two( Property, 20 );
                                         Source.Compute( Property );
                                         Source.Widen( y );
                                     }
                                 }
                                 """;

    [Fact]
    public async Task Redirect_NodeNotInSourceCompilation_Throws()
        => await this.ExecuteAsync(
            s =>
            {
                var foreignCallSite = (InvocationExpressionSyntax) SyntaxFactory.ParseExpression( "Source.Compute( 1 )" );

                Assert.Throws<ArgumentException>(
                    () => s.Factory.RedirectInvocation(
                        s.Origin,
                        new InvocationRedirectionRequest( foreignCallSite, s.Target( "Interceptors", "Compute" ), CallSiteReceiverMode.Drop ) ) );
            } );

    [Fact]
    public async Task RedirectTwice_Throws()
        => await this.ExecuteAsync(
            s =>
            {
                var request = new InvocationRedirectionRequest(
                    s.Invocation( "Source.Compute( 1 )" ),
                    s.Target( "Interceptors", "Compute" ),
                    CallSiteReceiverMode.Drop );

                s.Factory.RedirectInvocation( s.Origin, request );

                Assert.Throws<InvalidOperationException>( () => s.Factory.RedirectInvocation( s.Origin, request ) );
            } );

    [Fact]
    public async Task CompletedFactory_Throws()
    {
        ScriptContext? captured = null;
        InvocationRedirectionRequest? request = null;

        await this.ExecuteAsync(
            s =>
            {
                captured = s;

                request = new InvocationRedirectionRequest(
                    s.Invocation( "Source.Compute( 1 )" ),
                    s.Target( "Interceptors", "Compute" ),
                    CallSiteReceiverMode.Drop );
            } );

        Assert.NotNull( captured );
        Assert.Throws<InvalidOperationException>( () => captured.Factory.RedirectInvocation( captured.Origin, request! ) );
    }

    [Fact]
    public async Task UnknownLayer_Throws()
        => await this.ExecuteAsync(
            s =>
            {
                var origin = new ExtensionContributionOrigin(
                    s.Origin.Predecessor,
                    "unknown",
                    null,
                    null,
                    new AspectLayerId( "UnknownAspect" ),
                    null );

                Assert.Throws<ArgumentException>(
                    () => s.Factory.RedirectInvocation(
                        origin,
                        new InvocationRedirectionRequest(
                            s.Invocation( "Source.Compute( 1 )" ),
                            s.Target( "Interceptors", "Compute" ),
                            CallSiteReceiverMode.Drop ) ) );
            } );

    [Fact]
    public async Task RedirectInvocation_NonStaticTarget_Throws()
        => await this.AssertInvocationRefusedAsync( "Source.Compute( 1 )", "NonStatic", "Compute", CallSiteReceiverMode.Drop );

    [Fact]
    public async Task RedirectInvocation_Drop_InstanceReceiver_Throws()
        => await this.AssertInvocationRefusedAsync( "i.Get( 2 )", "Interceptors", "Compute", CallSiteReceiverMode.Drop );

    [Fact]
    public async Task RedirectInvocation_FirstArgument_StaticSource_Throws()
        => await this.AssertInvocationRefusedAsync( "Source.Compute( 1 )", "Interceptors", "Get", CallSiteReceiverMode.FirstArgument );

    [Fact]
    public async Task RedirectInvocation_FirstArgument_ConditionalAccess_Throws()
        => await this.AssertInvocationRefusedAsync( ".Get( 3 )", "Interceptors", "Get", CallSiteReceiverMode.FirstArgument );

    [Fact]
    public async Task RedirectInvocation_ExtensionReceiver_NonExtensionTarget_Throws()
        => await this.AssertInvocationRefusedAsync( "i.Get( 2 )", "Interceptors", "Get", CallSiteReceiverMode.ExtensionReceiver );

    [Fact]
    public async Task RedirectInvocation_ExtensionReceiver_ConditionalAccess_Accepted()
        => await this.ExecuteAsync(
            s =>
            {
                var callSite = s.Invocation( ".Get( 3 )" );

                s.Factory.RedirectInvocation(
                    s.Origin,
                    new InvocationRedirectionRequest( callSite, s.Target( "Interceptors", "GetExtension" ), CallSiteReceiverMode.ExtensionReceiver ) );

                Assert.True( s.Factory.IsRedirected( callSite ) );
            } );

    [Fact]
    public async Task RedirectInvocation_ResultCastInConditionalAccess_Throws()
        => await this.AssertInvocationRefusedAsync(
            ".Get( 7 )",
            "Interceptors",
            "GetExtension",
            CallSiteReceiverMode.ExtensionReceiver,
            ( s, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode ) { ResultCast = s.Compilation.Factory.GetSpecialType( Code.SpecialType.Int32 ) } );

    [Fact]
    public async Task RedirectInvocation_ArgumentsUseSourceValueTwice_Throws()
        => await this.AssertInvocationRefusedAsync(
            "Source.Two( 5, 6 )",
            "Interceptors",
            "Two",
            CallSiteReceiverMode.Drop,
            ( _, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode )
            {
                Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 0 ), RedirectedArgument.SourceArgument( 0 ) )
            } );

    [Fact]
    public async Task RedirectInvocation_ArgumentsNameParameterTwice_Throws()
        => await this.AssertInvocationRefusedAsync(
            "Source.Two( 5, 6 )",
            "Interceptors",
            "Two",
            CallSiteReceiverMode.Drop,
            ( _, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode )
            {
                Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 0 ).WithName( "a" ), RedirectedArgument.SourceArgument( 1 ).WithName( "a" ) )
            } );

    [Fact]
    public async Task RedirectInvocation_ArgumentsDropRefModifier_Throws()
        => await this.AssertInvocationRefusedAsync(
            "Source.Ref( ref y )",
            "Interceptors",
            "Ref",
            CallSiteReceiverMode.Drop,
            ( _, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode )
            {
                Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 0 ) )
            } );

    [Fact]
    public async Task RedirectInvocation_ArgumentsOmitOutArgument_Throws()
        => await this.AssertInvocationRefusedAsync(
            "Source.Out( 4, out var z )",
            "Interceptors",
            "Out",
            CallSiteReceiverMode.Drop,
            ( _, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode )
            {
                Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 0 ) )
            } );

    [Fact]
    public async Task RedirectInvocation_ArgumentsLongerThanParameters_Throws()
        => await this.AssertInvocationRefusedAsync(
            "Source.Compute( 1 )",
            "Interceptors",
            "Compute",
            CallSiteReceiverMode.Drop,
            ( s, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode )
            {
                Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 0 ), RedirectedArgument.Value( SyntaxFactory.ParseExpression( "2" ) ) )
            } );

    /// <summary>
    /// Verifies that a call can be redirected to the method that it already calls, with an additional named argument, and that the linker writes
    /// the argument.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_SameTargetWithExtraNamedArguments_Accepted()
    {
        var result = await this.ExecuteAsync(
            s => s.Factory.RedirectInvocation(
                s.Origin,
                new InvocationRedirectionRequest( s.Invocation( "Source.Log( \"message\" )" ), s.Target( "Source", "Log" ), CallSiteReceiverMode.Drop )
                {
                    ExtraArguments = ImmutableArray.Create( new CallSiteExtraArgument( "origin", SyntaxFactory.ParseExpression( "\"redirected\"" ) ) )
                } ) );

        // The linker output is not formatted, so the test ignores the whitespace.
        Assert.Contains( "Source.Log(\"message\",origin:\"redirected\")", GetText( result ).Replace( " ", "" ), StringComparison.Ordinal );
    }

    [Fact]
    public async Task RedirectMethodReference_NotConverted_Throws()
        => await this.ExecuteAsync(
            s => Assert.Throws<ArgumentException>(
                () => s.Factory.RedirectMethodReference(
                    s.Origin,
                    new MethodReferenceRedirectionRequest(
                        s.Node<MemberAccessExpressionSyntax>( "Source.Two", n => n.Parent.IsKind( SyntaxKind.Argument ) ),
                        s.Target( "Interceptors", "Two" ),
                        CallSiteReceiverMode.Drop ) ) ) );

    [Fact]
    public async Task RedirectMethodReference_InvokedExpression_Throws()
        => await this.ExecuteAsync(
            s => Assert.Throws<ArgumentException>(
                () => s.Factory.RedirectMethodReference(
                    s.Origin,
                    new MethodReferenceRedirectionRequest(
                        s.Invocation( "Source.Compute( 1 )" ).Expression,
                        s.Target( "Interceptors", "Compute" ),
                        CallSiteReceiverMode.Drop ) ) ) );

    [Fact]
    public async Task RedirectMethodReference_ReceiverModeOtherThanDrop_Throws()
        => await this.ExecuteAsync(
            s => Assert.Throws<ArgumentException>(
                () => s.Factory.RedirectMethodReference(
                    s.Origin,
                    new MethodReferenceRedirectionRequest( s.MethodGroup(), s.Target( "Interceptors", "Compute" ), CallSiteReceiverMode.FirstArgument ) ) ) );

    /// <summary>
    /// Verifies that <see cref="ExtensionTransformationFactory.IsRedirected"/> reports the requests, and that the name of a member access designates
    /// the member access.
    /// </summary>
    [Fact]
    public async Task IsRedirected_ReflectsRequests()
        => await this.ExecuteAsync(
            s =>
            {
                var callSite = s.Invocation( "Source.Compute( 1 )" );
                var methodGroup = s.MethodGroup();

                Assert.False( s.Factory.IsRedirected( callSite ) );
                Assert.False( s.Factory.IsRedirected( methodGroup ) );

                s.Factory.RedirectInvocation(
                    s.Origin,
                    new InvocationRedirectionRequest( callSite, s.Target( "Interceptors", "Compute" ), CallSiteReceiverMode.Drop ) );

                s.Factory.RedirectMethodReference(
                    s.Origin,
                    new MethodReferenceRedirectionRequest( methodGroup.Name, s.Target( "Interceptors", "Compute" ), CallSiteReceiverMode.Drop ) );

                Assert.True( s.Factory.IsRedirected( callSite ) );
                Assert.True( s.Factory.IsRedirected( methodGroup ) );
                Assert.True( s.Factory.IsRedirected( methodGroup.Name ) );
                Assert.False( s.Factory.IsRedirected( s.Invocation( "i.Get( 2 )" ) ) );
            } );

    /// <summary>
    /// Verifies that a dropped argument that can have a side effect, and that no argument follows, is evaluated after the previous argument: a
    /// property, which runs a getter, and a field of another object, which can throw.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_DroppedArgumentWithSideEffect_EvaluatedAfterPreviousArgument()
    {
        var result = await this.ExecuteAsync(
            s =>
            {
                foreach ( var callSiteText in new[] { "Source.Two( 10, Property )", "Source.Two( 12, i.Field )" } )
                {
                    s.Factory.RedirectInvocation(
                        s.Origin,
                        DropSecondArgument(
                            s,
                            new InvocationRedirectionRequest( s.Invocation( callSiteText ), s.Target( "Interceptors", "Compute" ), CallSiteReceiverMode.Drop ) ) );
                }
            } );

        var compactText = GetText( result ).Replace( " ", "" );

        Assert.Matches( @"Interceptors\.Compute\(x:10switch\{var(__value\d+)=>Propertyswitch\{_=>\1\}\}\)", compactText );
        Assert.Matches( @"Interceptors\.Compute\(x:12switch\{var(__value\d+)=>i\.Fieldswitch\{_=>\1\}\}\)", compactText );
    }

    /// <summary>
    /// Verifies that a dropped argument that can have a side effect is evaluated before the next argument.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_DroppedArgumentWithSideEffect_EvaluatedBeforeNextArgument()
    {
        var result = await this.ExecuteAsync(
            s => s.Factory.RedirectInvocation(
                s.Origin,
                new InvocationRedirectionRequest( s.Invocation( "Source.Two( Property, 20 )" ), s.Target( "Interceptors", "Compute" ), CallSiteReceiverMode.Drop )
                {
                    Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 1 ) )
                } ) );

        Assert.Contains( "Interceptors.Compute(x:Propertyswitch{_=>20})", GetText( result ).Replace( " ", "" ), StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that a dropped argument that can have a side effect is refused when the new call has no argument that can evaluate it.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_DroppedArgumentWithSideEffect_NoAdjacentArgument_Throws()
        => await this.AssertInvocationRefusedAsync(
            "Source.Compute( Property )",
            "Interceptors",
            "None",
            CallSiteReceiverMode.Drop,
            ( _, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode ) { Arguments = ImmutableArray<RedirectedArgument>.Empty } );

    /// <summary>
    /// Verifies that a source argument can be cast to the type of the parameter of the source method, and that the linker writes the cast.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_CastSourceArgument_WritesCast()
    {
        var result = await this.ExecuteAsync(
            s => s.Factory.RedirectInvocation(
                s.Origin,
                new InvocationRedirectionRequest( s.Invocation( "Source.Widen( y )" ), s.Target( "Interceptors", "Widen" ), CallSiteReceiverMode.Drop )
                {
                    Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 0 ).WithCast( s.Compilation.Factory.GetSpecialType( Code.SpecialType.Int64 ) ) )
                } ) );

        Assert.Contains( "Interceptors.Widen(value:(global::System.Int64)(y))", GetText( result ).Replace( " ", "" ), StringComparison.Ordinal );
    }

    [Fact]
    public async Task RedirectInvocation_CastValue_Throws()
        => await this.AssertInvocationRefusedAsync(
            "Source.Compute( 1 )",
            "Interceptors",
            "Compute",
            CallSiteReceiverMode.Drop,
            ( s, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode )
            {
                Arguments = ImmutableArray.Create(
                    RedirectedArgument.Value( SyntaxFactory.ParseExpression( "1" ) ).WithCast( s.Compilation.Factory.GetSpecialType( Code.SpecialType.Int32 ) ) )
            } );

    [Fact]
    public async Task RedirectInvocation_CastRefArgument_Throws()
        => await this.AssertInvocationRefusedAsync(
            "Source.Ref( ref y )",
            "Source",
            "Ref",
            CallSiteReceiverMode.Drop,
            ( s, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode )
            {
                Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 0 ).WithCast( s.Compilation.Factory.GetSpecialType( Code.SpecialType.Int32 ) ) )
            } );

    /// <summary>
    /// Verifies that a dropped argument is refused when it is passed by reference, because its evaluation cannot be separated from the call.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_DroppedRefArgument_Throws()
        => await this.AssertInvocationRefusedAsync(
            "Source.Ref( ref y )",
            "Interceptors",
            "None",
            CallSiteReceiverMode.Drop,
            ( _, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode ) { Arguments = ImmutableArray<RedirectedArgument>.Empty } );

    /// <summary>
    /// Verifies that an argument whose evaluation has no side effect can be dropped: a constant, and a field of <c>this</c>.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_DroppedArgumentWithoutSideEffect_Accepted()
        => await this.ExecuteAsync(
            s =>
            {
                foreach ( var callSiteText in new[] { "Source.Two( 11, Constant )", "Source.Two( 13, _field )" } )
                {
                    var request = DropSecondArgument(
                        s,
                        new InvocationRedirectionRequest( s.Invocation( callSiteText ), s.Target( "Interceptors", "Compute" ), CallSiteReceiverMode.Drop ) );

                    s.Factory.RedirectInvocation( s.Origin, request );

                    Assert.True( s.Factory.IsRedirected( request.CallSite ) );
                }
            } );

    [Fact]
    public async Task RedirectInvocation_DirectiveInInvokedExpression_Throws()
        => await this.ExecuteAsync(
            s => Assert.Throws<ArgumentException>(
                () => s.Factory.RedirectInvocation(
                    s.Origin,
                    new InvocationRedirectionRequest( s.InvocationWithArgument( "17" ), s.Target( "Interceptors", "Compute" ), CallSiteReceiverMode.Drop ) ) ) );

    [Fact]
    public async Task RedirectInvocation_DirectiveInPlannedArgumentList_Throws()
        => await this.ExecuteAsync(
            s => Assert.Throws<ArgumentException>(
                () => s.Factory.RedirectInvocation(
                    s.Origin,
                    new InvocationRedirectionRequest( s.InvocationWithArgument( "18" ), s.Target( "Interceptors", "Two" ), CallSiteReceiverMode.Drop )
                    {
                        Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 0 ), RedirectedArgument.SourceArgument( 1 ) )
                    } ) ) );

    /// <summary>
    /// Verifies that a directive in the argument list is kept when the arguments are not planned, because the linker keeps the argument list.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_DirectiveInArgumentListWithoutPlan_Kept()
    {
        var result = await this.ExecuteAsync(
            s => s.Factory.RedirectInvocation(
                s.Origin,
                new InvocationRedirectionRequest( s.InvocationWithArgument( "18" ), s.Target( "Interceptors", "Two" ), CallSiteReceiverMode.Drop ) ) );

        var text = GetText( result );

        Assert.Contains( "Interceptors.Two(18,", text.Replace( " ", "" ), StringComparison.Ordinal );
        Assert.Equal( 3, CountOccurrences( text, "#if DEBUG" ) );
    }

    [Fact]
    public async Task RedirectMethodReference_DirectiveInMethodGroup_Throws()
        => await this.ExecuteAsync(
            s => Assert.Throws<ArgumentException>(
                () => s.Factory.RedirectMethodReference(
                    s.Origin,
                    new MethodReferenceRedirectionRequest(
                        s.Node<MemberAccessExpressionSyntax>( null, n => n.ContainsDirectives && n.Parent.IsKind( SyntaxKind.EqualsValueClause ) ),
                        s.Target( "Interceptors", "Compute" ),
                        CallSiteReceiverMode.Drop ) ) ) );

    /// <summary>
    /// Verifies that the comments of the discarded parts of a call site are moved before the rewritten call site, that the comments of the reused
    /// parts are kept, and that no comment is duplicated.
    /// </summary>
    [Fact]
    public async Task Redirect_CommentsAreKeptOnce()
    {
        var result = await this.ExecuteAsync(
            s =>
            {
                s.Factory.RedirectInvocation(
                    s.Origin,
                    new InvocationRedirectionRequest( s.InvocationWithArgument( "14" ), s.Target( "Interceptors", "Compute" ), CallSiteReceiverMode.Drop ) );

                s.Factory.RedirectInvocation(
                    s.Origin,
                    new InvocationRedirectionRequest( s.InvocationWithArgument( "15" ), s.Target( "Interceptors", "Two" ), CallSiteReceiverMode.Drop )
                    {
                        Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 1 ).WithName( "a" ), RedirectedArgument.SourceArgument( 0 ).WithName( "b" ) )
                    } );

                s.Factory.RedirectMethodReference(
                    s.Origin,
                    new MethodReferenceRedirectionRequest(
                        s.Node<MemberAccessExpressionSyntax>( "Source /* group */ . Compute" ),
                        s.Target( "Interceptors", "Compute" ),
                        CallSiteReceiverMode.Drop ) );
            } );

        var text = GetText( result );
        var compactText = text.Replace( " ", "" );

        Assert.Contains( "/*callee*/global::Interceptors.Compute(14)", compactText, StringComparison.Ordinal );
        Assert.Contains( "g=/*group*/global::Interceptors.Compute;", compactText, StringComparison.Ordinal );

        // The comment after the separator of the source arguments is moved before the call, because the planned arguments get new separators.
        // The comment after the opening parenthesis is kept in place, because the parenthesis is kept.
        var reorderedCallIndex = compactText.IndexOf( "global::Interceptors.Two(/*first*/b:15,a:16)", StringComparison.Ordinal );
        Assert.True( reorderedCallIndex > 0 );
        Assert.True( compactText.LastIndexOf( "//second", reorderedCallIndex, StringComparison.Ordinal ) > 0 );

        foreach ( var comment in new[] { "/* callee */", "/* first */", "// second", "/* group */" } )
        {
            Assert.Equal( 1, CountOccurrences( text, comment ) );
        }
    }

    [Fact]
    public async Task ExtensionTemplateServices_MethodTemplateExists()
        => await this.ExecuteAsync(
            s =>
            {
                var serviceProvider = s.Context.ServiceProvider;
                var aspectProvider = s.Origin.DefaultTemplateProvider!.Value;

                Assert.True( ExtensionTemplateServices.MethodTemplateExists( serviceProvider, aspectProvider, "Template" ) );
                Assert.False( ExtensionTemplateServices.MethodTemplateExists( serviceProvider, aspectProvider, "NotTemplate" ) );
                Assert.False( ExtensionTemplateServices.MethodTemplateExists( serviceProvider, aspectProvider, "Missing" ) );
                Assert.False( ExtensionTemplateServices.MethodTemplateExists( serviceProvider, TemplateProvider.FromInstanceUnsafe( new object() ), "Template" ) );
                Assert.False( ExtensionTemplateServices.MethodTemplateExists( serviceProvider, default, "Template" ) );
            } );

    /// <summary>
    /// Customizes a request so that it passes the first source argument and drops the second one.
    /// </summary>
    private static InvocationRedirectionRequest DropSecondArgument( ScriptContext s, InvocationRedirectionRequest r )
        => new( r.CallSite, r.Target, r.ReceiverMode ) { Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 0 ) ) };

    private static string GetText( CompileTimeAspectPipelineResult result )
        => string.Concat( result.ResultingCompilation.SyntaxTreeCollection.SelectAsReadOnlyCollection( t => t.ToString() ) );

    private static int CountOccurrences( string text, string value )
    {
        var count = 0;

        for ( var index = text.IndexOf( value, StringComparison.Ordinal ); index >= 0; index = text.IndexOf( value, index + 1, StringComparison.Ordinal ) )
        {
            count++;
        }

        return count;
    }

    private Task AssertInvocationRefusedAsync(
        string callSiteText,
        string targetType,
        string targetMethod,
        CallSiteReceiverMode receiverMode,
        Func<ScriptContext, InvocationRedirectionRequest, InvocationRedirectionRequest>? customize = null )
        => this.ExecuteAsync(
            s =>
            {
                var request = new InvocationRedirectionRequest( s.Invocation( callSiteText ), s.Target( targetType, targetMethod ), receiverMode );

                if ( customize != null )
                {
                    request = customize( s, request );
                }

                Assert.Throws<ArgumentException>( () => s.Factory.RedirectInvocation( s.Origin, request ) );
                Assert.False( s.Factory.IsRedirected( request.CallSite ) );
            } );

    private async Task<CompileTimeAspectPipelineResult> ExecuteAsync( Action<ScriptContext> script )
    {
        var scriptService = new Script( script );
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( scriptService );

        using var testContext = this.CreateTestContext(
            this.CreateDefaultTestContextOptions() with { ExtensionTypes = ImmutableArray.Create( typeof(ScriptedExtension) ) },
            additionalServices );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var compilation = testContext.CreateCSharpCompilation( _code );
        var diagnostics = new List<Diagnostic>();

        var result = await pipeline.ExecuteAsync( diagnostics.Add, null, compilation, ImmutableArray<ManagedResource>.Empty );

        Assert.True( result.IsSuccessful, string.Join( Environment.NewLine, diagnostics ) );
        Assert.True( scriptService.HasRun );

        return result.Value;
    }

    /// <summary>
    /// The project service that holds the script of a test.
    /// </summary>
    private sealed class Script : IProjectService
    {
        public Script( Action<ScriptContext> action )
        {
            this.Action = action;
        }

        public Action<ScriptContext> Action { get; }

        public bool HasRun { get; set; }
    }

    /// <summary>
    /// The objects that a script uses, with helpers that find the nodes of the test code.
    /// </summary>
    private sealed record ScriptContext( ExtensionTransformationContext Context, ExtensionTransformationFactory Factory, ExtensionContributionOrigin Origin )
    {
        public CompilationModel Compilation => this.Context.StageFinalCompilation;

        public InvocationExpressionSyntax Invocation( string text ) => this.Node<InvocationExpressionSyntax>( text );

        /// <summary>
        /// Gets the invocation whose first argument has the given text, for the invocations whose text spans several lines.
        /// </summary>
        public InvocationExpressionSyntax InvocationWithArgument( string firstArgument )
            => this.Compilation.PartialCompilation.SyntaxTreeCollection
                .SelectMany( t => t.GetRoot().DescendantNodes() )
                .OfType<InvocationExpressionSyntax>()
                .Single( n => n.ArgumentList.Arguments is [{ } argument, ..] && argument.Expression.ToString() == firstArgument );

        /// <summary>
        /// Gets the method group <c>Source.Compute</c> that is converted to a delegate.
        /// </summary>
        public MemberAccessExpressionSyntax MethodGroup()
            => this.Node<MemberAccessExpressionSyntax>( "Source.Compute", n => n.Parent.IsKind( SyntaxKind.EqualsValueClause ) );

        public T Node<T>( string? text, Func<T, bool>? predicate = null )
            where T : SyntaxNode
            => this.Compilation.PartialCompilation.SyntaxTreeCollection
                .SelectMany( t => t.GetRoot().DescendantNodes() )
                .OfType<T>()
                .Single( n => (text == null || n.ToString() == text) && (predicate == null || predicate( n )) );

        public CallSiteRedirectionTarget Target( string typeName, string methodName )
            => CallSiteRedirectionTarget.Existing( this.Compilation.Types.OfName( typeName ).Single().Methods.OfName( methodName ).Single() );
    }

    /// <summary>
    /// An extension whose transforming hook runs the <see cref="Script"/> of the project.
    /// </summary>
    private sealed class ScriptedExtension : PipelineExtension
    {
        public override Task ExecuteTransformingContributorsAsync( ExtensionTransformationContext context, CancellationToken cancellationToken )
        {
            var script = context.ServiceProvider.GetService<Script>();

            if ( script == null )
            {
                return Task.CompletedTask;
            }

            var typeC = context.SourceCompilationWithFinalAspects.Types.OfName( "C" ).Single();
            var aspectInstance = (IAspectInstanceInternal) typeC.Enhancements().GetAspectInstances().Single();

            var origin = new ExtensionContributionOrigin(
                aspectInstance.Predecessors[0],
                aspectInstance.ToString()!,
                TemplateProvider.FromInstance( aspectInstance.Aspect ),
                null,
                new AspectLayerId( aspectInstance.AspectClass ),
                aspectInstance );

            script.Action( new ScriptContext( context, context.TransformationFactory, origin ) );
            script.HasRun = true;

            return Task.CompletedTask;
        }
    }
}
