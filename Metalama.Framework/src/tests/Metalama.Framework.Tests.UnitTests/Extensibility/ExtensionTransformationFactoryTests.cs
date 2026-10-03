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
    /// <summary>
    /// The code of every test. It declares the aspect applied to the type <c>C</c>, the source methods, the target methods of the redirections,
    /// and the call sites in the members of <c>C</c>.
    /// </summary>
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

                                     public static int Make( Instance a, int b ) => b;

                                     public static string Join( string a, string b ) => a + b;

                                     public static int Many( int a, params int[] values ) => a;

                                     public static int Measure( int a, Meters b ) => a;
                                 }

                                 internal struct Meters
                                 {
                                     public static implicit operator Meters( int value ) => default;
                                 }

                                 internal class Instance
                                 {
                                     public int Field;

                                     public int Get( int x ) => x;

                                     public int Self() => Get( 40 );
                                 }

                                 internal class Outer
                                 {
                                     public Instance Inner = new();
                                 }

                                 internal struct Point
                                 {
                                     public int Get() => 0;

                                     public readonly int Read() => 0;
                                 }

                                 internal class VirtualBase
                                 {
                                     public virtual int Virtual() => 0;
                                 }

                                 internal class VirtualDerived : VirtualBase
                                 {
                                     public override int Virtual() => base.Virtual();
                                 }

                                 internal class ReadOnlyHolder
                                 {
                                     private readonly Point _point;

                                     public int M() => _point.Get();
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

                                     public static int Pick( int x ) => x;

                                     public static int Pick( int x, int y = 0 ) => x;

                                     public static int GetPoint( this Point p ) => 1;

                                     public static int GetPointByRef( ref Point p ) => 1;

                                     public static int ReadPoint( in Point p ) => 1;

                                     public static int TakeBaseByRef( ref VirtualBase instance ) => 1;

                                     public static int TakeBaseExtension( this VirtualBase instance ) => 1;

                                     public static int TakeTwo( Instance a, Instance b ) => 1;

                                     public static int ManyOne( int a, params int[] values ) => a;

                                     public static int TakeBase( VirtualBase instance ) => 1;

                                     public static void LogVoid( string message ) { }

                                     public static int Over( int x ) => x;

                                     public static int Over( long x ) => 0;

                                     public static int Over2( int a, int b ) => a;

                                     public static int Over2( long a, long b ) => 0;

                                     public static int MakeOne( Instance a ) => 0;

                                     public static string JoinOne( string a ) => a;

                                     public static void RefIn( in int x ) { }

                                     public static int ComputeRefReadOnly( ref readonly int x ) => x;
                                 }

                                 internal static class InstanceExtensions
                                 {
                                     public static int Get( this Instance instance, int x ) => x;
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
                                         Outer? o = null;
                                         o?.Inner.Get( 30 );
                                         Source.Two( 31, (Next()) );
                                         Source.Join( "32", NextString()! );
                                         Source.Make( new(), Next() );
                                         Source.Many( 1, 2, 3 );
                                         Source.Many( 33, Next() );
                                         Source.Measure( 34, Next() );
                                         var natural = Source.Two;
                                     }

                                     private unsafe void P()
                                     {
                                         Point pt = default;
                                         Point* pp = &pt;
                                         pp->Get();
                                         pt.Read();
                                     }

                                     private static int Next() => 0;

                                     private static string? NextString() => null;
                                 }
                                 """;

    /// <summary>
    /// Verifies that a call site that does not belong to a syntax tree of the compilation of the stage is refused with an
    /// <see cref="ArgumentException"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that a second request for the same call site throws an <see cref="InvalidOperationException"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that the factory throws an <see cref="InvalidOperationException"/> when it is used after the transforming hook has completed.
    /// </summary>
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

    /// <summary>
    /// Verifies that a request is refused when the aspect layer of its origin is not an ordered layer of the pipeline.
    /// </summary>
    [Fact]
    public async Task UnknownLayer_Throws()
        => await this.ExecuteAsync(
            s =>
            {
                var origin = new ExtensionContributionOrigin(
                    s.Origin.Predecessor,
                    "unknown",
                    default,
                    null,
                    new AspectLayerId( "UnknownAspect" ),
                    null,
                    0 );

                Assert.Throws<ArgumentException>(
                    () => s.Factory.RedirectInvocation(
                        origin,
                        new InvocationRedirectionRequest(
                            s.Invocation( "Source.Compute( 1 )" ),
                            s.Target( "Interceptors", "Compute" ),
                            CallSiteReceiverMode.Drop ) ) );
            } );

    /// <summary>
    /// Verifies that a request whose target method is not static is refused.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_NonStaticTarget_Throws()
        => await this.AssertInvocationRefusedAsync( "Source.Compute( 1 )", "NonStatic", "Compute", CallSiteReceiverMode.Drop );

    /// <summary>
    /// Verifies that the receiver mode <see cref="CallSiteReceiverMode.Drop"/> is refused for a call that has an instance receiver, when the request
    /// does not plan the arguments.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_Drop_InstanceReceiver_Throws()
        => await this.AssertInvocationRefusedAsync( "i.Get( 2 )", "Interceptors", "Compute", CallSiteReceiverMode.Drop );

    /// <summary>
    /// Verifies that the receiver mode <see cref="CallSiteReceiverMode.FirstArgument"/> is refused for a call to a static method, which has
    /// no receiver to pass.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_FirstArgument_StaticSource_Throws()
        => await this.AssertInvocationRefusedAsync( "Source.Compute( 1 )", "Interceptors", "Get", CallSiteReceiverMode.FirstArgument );

    /// <summary>
    /// Verifies that the receiver mode <see cref="CallSiteReceiverMode.FirstArgument"/> is refused for a call in a conditional access, whose receiver
    /// exists only inside the conditional access.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_FirstArgument_ConditionalAccess_Throws()
        => await this.AssertInvocationRefusedAsync( ".Get( 3 )", "Interceptors", "Get", CallSiteReceiverMode.FirstArgument );

    /// <summary>
    /// Verifies that the receiver mode <see cref="CallSiteReceiverMode.ExtensionReceiver"/> is refused when the target method is not an extension
    /// method.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_ExtensionReceiver_NonExtensionTarget_Throws()
        => await this.AssertInvocationRefusedAsync( "i.Get( 2 )", "Interceptors", "Get", CallSiteReceiverMode.ExtensionReceiver );

    /// <summary>
    /// Verifies that the receiver mode <see cref="CallSiteReceiverMode.ExtensionReceiver"/> is accepted for a call in a conditional access.
    /// </summary>
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

    /// <summary>
    /// Verifies that a result cast is refused for a call in a conditional access.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_ResultCastInConditionalAccess_Throws()
        => await this.AssertInvocationRefusedAsync(
            ".Get( 7 )",
            "Interceptors",
            "GetExtension",
            CallSiteReceiverMode.ExtensionReceiver,
            ( s, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode ) { ResultCast = s.Compilation.Factory.GetSpecialType( Code.SpecialType.Int32 ) } );

    /// <summary>
    /// Verifies that an argument list that passes the same source argument twice is refused.
    /// </summary>
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

    /// <summary>
    /// Verifies that an argument list that names the same parameter twice is refused.
    /// </summary>
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

    /// <summary>
    /// Verifies that a source argument passed with the <c>ref</c> modifier is refused when the parameter of the target method is passed by value.
    /// </summary>
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

    /// <summary>
    /// Verifies that an argument list that omits an <c>out</c> argument of the source call site is refused.
    /// </summary>
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

    /// <summary>
    /// Verifies that an argument list that has more elements than the parameters of the target method is refused.
    /// </summary>
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

    /// <summary>
    /// Verifies that a method group that is not converted to a delegate or to a function pointer, here the operand of <c>nameof</c>, is refused.
    /// </summary>
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

    /// <summary>
    /// Verifies that the invoked expression of an invocation is refused as a method reference.
    /// </summary>
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

    /// <summary>
    /// Verifies that a method reference is refused with a receiver mode other than <see cref="CallSiteReceiverMode.Drop"/>.
    /// </summary>
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

        Assert.Matches( @"Interceptors\.Compute\(x:(global::)?(Metalama\.Framework\.RunTime\.)?CallSiteHelper\.DropAfter\(10,Property\)\)", compactText );
        Assert.Matches( @"Interceptors\.Compute\(x:(global::)?(Metalama\.Framework\.RunTime\.)?CallSiteHelper\.DropAfter\(12,i\.Field\)\)", compactText );
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

        Assert.Matches( @"Interceptors\.Compute\(x:(global::)?(Metalama\.Framework\.RunTime\.)?CallSiteHelper\.DropBefore\(Property,20\)\)", GetText( result ).Replace( " ", "" ) );
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
    /// Verifies that a request is refused when the rewritten call binds to an overload of the target, here an overload without the optional
    /// parameter of the target.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_BindsToOtherOverload_Throws()
        => await this.ExecuteAsync(
            s =>
            {
                var exception = Assert.Throws<ArgumentException>(
                    () => s.Factory.RedirectInvocation(
                        s.Origin,
                        new InvocationRedirectionRequest(
                            s.Invocation( "Source.Compute( 1 )" ),
                            s.Target( "Interceptors", "Pick", m => m.Parameters.Count == 2 ),
                            CallSiteReceiverMode.Drop ) ) );

                Assert.Contains( "binds to 'Interceptors.Pick(int)'", exception.Message, StringComparison.Ordinal );
            } );

    /// <summary>
    /// Verifies that a request in a conditional access is refused when the rewritten call binds to an instance method of the receiver instead of
    /// the extension method of the target.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_ExtensionReceiver_BindsToInstanceMethod_Throws()
        => await this.ExecuteAsync(
            s =>
            {
                var exception = Assert.Throws<ArgumentException>(
                    () => s.Factory.RedirectInvocation(
                        s.Origin,
                        new InvocationRedirectionRequest( s.Invocation( ".Get( 3 )" ), s.Target( "InstanceExtensions", "Get" ), CallSiteReceiverMode.ExtensionReceiver ) ) );

                Assert.Contains( "binds to 'Instance.Get(int)'", exception.Message, StringComparison.Ordinal );
            } );

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

    /// <summary>
    /// Verifies that a cast is refused on an argument that is an expression emitted at the call site, because only a source argument can be cast.
    /// </summary>
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

    /// <summary>
    /// Verifies that a cast is refused on a source argument that is passed by reference.
    /// </summary>
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

    /// <summary>
    /// Verifies that a call site whose invoked expression contains a preprocessor directive is refused.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_DirectiveInInvokedExpression_Throws()
        => await this.ExecuteAsync(
            s => Assert.Throws<ArgumentException>(
                () => s.Factory.RedirectInvocation(
                    s.Origin,
                    new InvocationRedirectionRequest( s.InvocationWithArgument( "17" ), s.Target( "Interceptors", "Compute" ), CallSiteReceiverMode.Drop ) ) ) );

    /// <summary>
    /// Verifies that a call site whose argument list contains a preprocessor directive is refused when the request plans the arguments.
    /// </summary>
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

    /// <summary>
    /// Verifies that a method group that contains a preprocessor directive is refused.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExtensionTemplateServices.MethodTemplateExists"/> returns <c>true</c> only for a method of the template provider that
    /// is a template, and <c>false</c> for a method that is not a template, for a missing method, for a provider that is not a template provider, and
    /// for the default provider.
    /// </summary>
    [Fact]
    public async Task ExtensionTemplateServices_MethodTemplateExists()
        => await this.ExecuteAsync(
            s =>
            {
                var serviceProvider = s.Context.ServiceProvider;
                var aspectProvider = s.Origin.DefaultTemplateProvider;

                Assert.True( ExtensionTemplateServices.MethodTemplateExists( serviceProvider, aspectProvider, "Template" ) );
                Assert.False( ExtensionTemplateServices.MethodTemplateExists( serviceProvider, aspectProvider, "NotTemplate" ) );
                Assert.False( ExtensionTemplateServices.MethodTemplateExists( serviceProvider, aspectProvider, "Missing" ) );
                Assert.False( ExtensionTemplateServices.MethodTemplateExists( serviceProvider, TemplateProvider.FromInstanceUnsafe( new object() ), "Template" ) );
                Assert.False( ExtensionTemplateServices.MethodTemplateExists( serviceProvider, default, "Template" ) );
            } );

    /// <summary>
    /// Verifies that a call in a chain that starts with a conditional access, <c>o?.Inner.Get( 30 )</c>, is a conditional access: its receiver
    /// cannot be passed as an argument.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_FirstArgument_ConditionalAccessChain_Throws()
        => await this.AssertInvocationRefusedAsync(
            ".Inner.Get( 30 )",
            "Interceptors",
            "Get",
            CallSiteReceiverMode.FirstArgument,
            expectedMessage: "conditional access" );

    /// <summary>
    /// Verifies that a call in a chain that starts with a conditional access can be redirected to an extension method, which is the rewrite that
    /// keeps the conditional access.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_ExtensionReceiver_ConditionalAccessChain_Accepted()
    {
        var result = await this.ExecuteAsync(
            s => s.Factory.RedirectInvocation(
                s.Origin,
                new InvocationRedirectionRequest( s.Invocation( ".Inner.Get( 30 )" ), s.Target( "Interceptors", "GetExtension" ), CallSiteReceiverMode.ExtensionReceiver ) ) );

        Assert.Contains( "o?.Inner.GetExtension(30)", GetText( result ).Replace( " ", "" ), StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that the receiver of a base call to a virtual method cannot be passed as a source argument, because the target would call the
    /// method with a virtual dispatch.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_Drop_SourceReceiver_VirtualBaseCall_Throws()
        => await this.AssertInvocationRefusedAsync(
            "base.Virtual()",
            "Interceptors",
            "TakeBase",
            CallSiteReceiverMode.Drop,
            ( _, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode ) { Arguments = ImmutableArray.Create( RedirectedArgument.SourceReceiver ) },
            expectedMessage: "base call" );

    /// <summary>
    /// Verifies that the receiver of a pointer member access is kept when the call is redirected to an extension method.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_ExtensionReceiver_PointerMemberAccess_DereferencesPointer()
    {
        var result = await this.ExecuteAsync(
            s => s.Factory.RedirectInvocation(
                s.Origin,
                new InvocationRedirectionRequest( s.Invocation( "pp->Get()" ), s.Target( "Interceptors", "GetPoint" ), CallSiteReceiverMode.ExtensionReceiver ) ) );

        Assert.Contains( "(*(pp)).GetPoint()", GetText( result ).Replace( " ", "" ), StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that a result cast is refused when the target method returns <c>void</c>.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_ResultCast_VoidTarget_Throws()
        => await this.AssertInvocationRefusedAsync(
            "Source.Log( \"message\" )",
            "Interceptors",
            "LogVoid",
            CallSiteReceiverMode.Drop,
            ( s, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode )
            {
                ResultCast = s.Compilation.Factory.GetSpecialType( Code.SpecialType.Int32 )
            },
            expectedMessage: "result cast" );

    /// <summary>
    /// Verifies that a result cast is refused when the call is an expression statement, because a cast expression is not a statement.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_ResultCast_ExpressionStatement_Throws()
        => await this.AssertInvocationRefusedAsync(
            "Source.Compute( 1 )",
            "Interceptors",
            "Compute",
            CallSiteReceiverMode.Drop,
            ( s, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode )
            {
                ResultCast = s.Compilation.Factory.GetSpecialType( Code.SpecialType.Int64 )
            },
            expectedMessage: "result cast" );

    /// <summary>
    /// Verifies that a dropped argument that is parenthesized, or whose nullability is suppressed, is evaluated like the same argument without
    /// the parentheses or the suppression.
    /// </summary>
    [Theory]
    [InlineData( "Source.Two( 31, (Next()) )", "Compute" )]
    [InlineData( "Source.Join( \"32\", NextString()! )", "JoinOne" )]
    public async Task RedirectInvocation_DroppedArgumentInParenthesesOrSuppression_Accepted( string callSiteText, string targetName )
    {
        var result = await this.ExecuteAsync(
            s => s.Factory.RedirectInvocation(
                s.Origin,
                DropSecondArgument(
                    s,
                    new InvocationRedirectionRequest( s.Invocation( callSiteText ), s.Target( "Interceptors", targetName ), CallSiteReceiverMode.Drop ) ) ) );

        Assert.Contains( $"Interceptors.{targetName}(", GetText( result ), StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that a target-typed value, which has no natural type, holds a following dropped value. Both type arguments of the helper are
    /// written, so that the value is converted to the type of the parameter as in the original call.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_DroppedArgumentAfterTargetTypedValue_ExplicitTypeArguments()
    {
        var result = await this.ExecuteAsync(
            s => s.Factory.RedirectInvocation(
                s.Origin,
                DropSecondArgument(
                    s,
                    new InvocationRedirectionRequest( s.Invocation( "Source.Make( new(), Next() )" ), s.Target( "Interceptors", "MakeOne" ), CallSiteReceiverMode.Drop ) ) ) );

        Assert.Matches( @"DropAfter<(global::)?Instance,(int|global::System\.Int32)>\(new\(\),Next\(\)\)", GetText( result ).Replace( " ", "" ) );
    }

    /// <summary>
    /// Verifies that a method group is not redirected when the method group of the target binds to another overload of the target.
    /// </summary>
    [Fact]
    public async Task RedirectMethodReference_BindsToOtherOverload_Throws()
        => await this.ExecuteAsync(
            s =>
            {
                var exception = Assert.Throws<ArgumentException>(
                    () => s.Factory.RedirectMethodReference(
                        s.Origin,
                        new MethodReferenceRedirectionRequest( s.MethodGroup(), s.Target( "Interceptors", "Over", m => m.Parameters[0].Type.SpecialType == Code.SpecialType.Int64 ), CallSiteReceiverMode.Drop ) ) );

                Assert.Contains( "binds to", exception.Message, StringComparison.Ordinal );
            } );

    /// <summary>
    /// Verifies that a method group whose delegate type is its natural type is not redirected to an overloaded method group, which has no
    /// natural type.
    /// </summary>
    [Fact]
    public async Task RedirectMethodReference_NaturalTypeOfOverloadedTarget_Throws()
        => await this.ExecuteAsync(
            s => Assert.Throws<ArgumentException>(
                () => s.Factory.RedirectMethodReference(
                    s.Origin,
                    new MethodReferenceRedirectionRequest(
                        s.Node<MemberAccessExpressionSyntax>( "Source.Two", n => n.Parent.IsKind( SyntaxKind.EqualsValueClause ) ),
                        s.Target( "Interceptors", "Over2", m => m.Parameters[0].Type.SpecialType == Code.SpecialType.Int32 ),
                        CallSiteReceiverMode.Drop ) ) ) );

    /// <summary>
    /// Verifies that a source argument is not passed to a parameter whose reference kind differs from the reference kind of the argument, except a
    /// value passed to an <c>in</c> parameter. The other pairs produce warnings CS9191 to CS9193 in the rewritten call.
    /// </summary>
    [Theory]
    [InlineData( "Source.Ref( ref y )", "RefIn" )]
    [InlineData( "Source.Compute( 1 )", "ComputeRefReadOnly" )]
    public async Task RedirectInvocation_ArgumentsWithDifferentRefKind_Throws( string callSiteText, string targetName )
        => await this.AssertInvocationRefusedAsync(
            callSiteText,
            "Interceptors",
            targetName,
            CallSiteReceiverMode.Drop,
            ( _, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode ) { Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 0 ) ) } );

    /// <summary>
    /// Verifies that <see cref="ExtensionTransformationFactory.RedirectInvocation"/> throws an <see cref="ArgumentNullException"/> when the request is
    /// <c>null</c>.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_NullRequest_Throws()
        => await this.ExecuteAsync( s => Assert.Throws<ArgumentNullException>( () => s.Factory.RedirectInvocation( s.Origin, null! ) ) );

    /// <summary>
    /// Verifies that <see cref="ExtensionTransformationFactory.RedirectInvocation"/> throws an <see cref="ArgumentNullException"/> when the origin is
    /// <c>null</c>.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_NullOrigin_Throws()
        => await this.ExecuteAsync(
            s => Assert.Throws<ArgumentNullException>(
                () => s.Factory.RedirectInvocation(
                    null!,
                    new InvocationRedirectionRequest( s.Invocation( "Source.Compute( 1 )" ), s.Target( "Interceptors", "Compute" ), CallSiteReceiverMode.Drop ) ) ) );

    /// <summary>
    /// Verifies that <see cref="ExtensionTransformationFactory.RedirectMethodReference"/> throws an <see cref="ArgumentNullException"/> when the
    /// request is <c>null</c>.
    /// </summary>
    [Fact]
    public async Task RedirectMethodReference_NullRequest_Throws()
        => await this.ExecuteAsync( s => Assert.Throws<ArgumentNullException>( () => s.Factory.RedirectMethodReference( s.Origin, null! ) ) );

    /// <summary>
    /// Verifies that the receiver of a base call to a virtual method is refused with every receiver mode that passes it.
    /// </summary>
    [Theory]
    [InlineData( CallSiteReceiverMode.FirstArgument, "TakeBase" )]
    [InlineData( CallSiteReceiverMode.FirstArgumentByRef, "TakeBaseByRef" )]
    [InlineData( CallSiteReceiverMode.ExtensionReceiver, "TakeBaseExtension" )]
    public async Task RedirectInvocation_VirtualBaseCall_AnyReceiverMode_Throws( CallSiteReceiverMode receiverMode, string targetName )
        => await this.AssertInvocationRefusedAsync( "base.Virtual()", "Interceptors", targetName, receiverMode, expectedMessage: "base call" );

    /// <summary>
    /// Verifies that the receiver cannot be passed both by the receiver mode and as a planned argument, nor twice as a planned argument.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_SourceReceiverPassedTwice_Throws()
    {
        await this.AssertInvocationRefusedAsync(
            "i.Get( 2 )",
            "Interceptors",
            "TakeTwo",
            CallSiteReceiverMode.FirstArgument,
            ( _, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode )
            {
                Arguments = ImmutableArray.Create( RedirectedArgument.SourceReceiver.WithName( "b" ) )
            } );

        await this.AssertInvocationRefusedAsync(
            "i.Get( 2 )",
            "Interceptors",
            "TakeTwo",
            CallSiteReceiverMode.Drop,
            ( _, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode )
            {
                Arguments = ImmutableArray.Create( RedirectedArgument.SourceReceiver.WithName( "a" ), RedirectedArgument.SourceReceiver.WithName( "b" ) )
            } );
    }

    /// <summary>
    /// Verifies that the elements of an expanded <c>params</c> argument cannot be cast, because they are packed into one collection.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_CastExpandedParams_Throws()
        => await this.AssertInvocationRefusedAsync(
            "Source.Many( 1, 2, 3 )",
            "Interceptors",
            "ManyOne",
            CallSiteReceiverMode.Drop,
            ( s, r ) => new InvocationRedirectionRequest( r.CallSite, r.Target, r.ReceiverMode )
            {
                Arguments = ImmutableArray.Create(
                    RedirectedArgument.SourceArgument( 0 ),
                    RedirectedArgument.SourceArgument( 1 ).WithCast( s.Compilation.Factory.GetSpecialType( Code.SpecialType.Int32 ) ) )
            },
            expectedMessage: "cannot be cast" );

    /// <summary>
    /// Verifies that the elements of an expanded <c>params</c> argument that the plan omits are not evaluated when they have no side effect, and
    /// are evaluated into a discard when they have one.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_ParamsArgumentOmitted()
    {
        var result = await this.ExecuteAsync(
            s =>
            {
                foreach ( var callSiteText in new[] { "Source.Many( 1, 2, 3 )", "Source.Many( 33, Next() )" } )
                {
                    s.Factory.RedirectInvocation(
                        s.Origin,
                        new InvocationRedirectionRequest( s.Invocation( callSiteText ), s.Target( "Interceptors", "Compute" ), CallSiteReceiverMode.Drop )
                        {
                            Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 0 ) )
                        } );
                }
            } );

        var compactText = GetText( result ).Replace( " ", "" );

        Assert.Contains( "Interceptors.Compute(x:1)", compactText, StringComparison.Ordinal );
        Assert.Matches( @"Interceptors\.Compute\(x:(global::)?(Metalama\.Framework\.RunTime\.)?CallSiteHelper\.DropAfter\(33,Next\(\)\)\)", compactText );
    }

    /// <summary>
    /// Verifies that an omitted argument whose conversion to the type of the parameter calls a user-defined operator is refused, because the
    /// discard would not call the operator.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_DroppedArgumentWithUserDefinedConversion_Throws()
        => await this.AssertInvocationRefusedAsync(
            "Source.Measure( 34, Next() )",
            "Interceptors",
            "Compute",
            CallSiteReceiverMode.Drop,
            DropSecondArgument,
            expectedMessage: "user-defined operator" );

    /// <summary>
    /// Verifies that a call with an implicit receiver is redirected to an extension method called on <c>this</c>.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_ExtensionReceiver_ImplicitThis()
    {
        var result = await this.ExecuteAsync(
            s => s.Factory.RedirectInvocation(
                s.Origin,
                new InvocationRedirectionRequest( s.Invocation( "Get( 40 )" ), s.Target( "Interceptors", "GetExtension" ), CallSiteReceiverMode.ExtensionReceiver ) ) );

        Assert.Contains( "this.GetExtension(40)", GetText( result ).Replace( " ", "" ), StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that the receiver of a call to a readonly member of a struct is passed with the <c>in</c> modifier.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_FirstArgumentByIn()
    {
        var result = await this.ExecuteAsync(
            s => s.Factory.RedirectInvocation(
                s.Origin,
                new InvocationRedirectionRequest( s.Invocation( "pt.Read()" ), s.Target( "Interceptors", "ReadPoint" ), CallSiteReceiverMode.FirstArgumentByIn ) ) );

        Assert.Contains( "Interceptors.ReadPoint(inpt)", GetText( result ).Replace( " ", "" ), StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that a receiver that is not a writable variable, here a readonly field outside of a constructor, cannot be passed by reference.
    /// </summary>
    [Fact]
    public async Task RedirectInvocation_FirstArgumentByRef_ReadOnlyReceiver_Throws()
        => await this.AssertInvocationRefusedAsync(
            "_point.Get()",
            "Interceptors",
            "GetPointByRef",
            CallSiteReceiverMode.FirstArgumentByRef,
            expectedMessage: "writable variable" );

    /// <summary>
    /// Customizes a request so that it passes the first source argument and drops the second one.
    /// </summary>
    private static InvocationRedirectionRequest DropSecondArgument( ScriptContext s, InvocationRedirectionRequest r )
        => new( r.CallSite, r.Target, r.ReceiverMode ) { Arguments = ImmutableArray.Create( RedirectedArgument.SourceArgument( 0 ) ) };

    /// <summary>
    /// Returns the concatenated text of the syntax trees of the resulting compilation.
    /// </summary>
    private static string GetText( CompileTimeAspectPipelineResult result )
        => string.Concat( result.ResultingCompilation.SyntaxTreeCollection.SelectAsReadOnlyCollection( t => t.ToString() ) );

    /// <summary>
    /// Returns the number of occurrences of <paramref name="value"/> in <paramref name="text"/>, including the overlapping ones, with an
    /// ordinal comparison.
    /// </summary>
    private static int CountOccurrences( string text, string value )
    {
        var count = 0;

        for ( var index = text.IndexOf( value, StringComparison.Ordinal ); index >= 0; index = text.IndexOf( value, index + 1, StringComparison.Ordinal ) )
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Runs a script that requests the redirection of a call site and asserts that the factory refuses the request with an
    /// <see cref="ArgumentException"/> and does not record the redirection.
    /// </summary>
    /// <param name="callSiteText">The text of the invocation in the test code.</param>
    /// <param name="targetType">The name of the type that declares the target method.</param>
    /// <param name="targetMethod">The name of the target method, which must not be overloaded.</param>
    /// <param name="receiverMode">The receiver mode of the request.</param>
    /// <param name="customize">A delegate that returns a customized copy of the request, or <c>null</c>.</param>
    /// <param name="expectedMessage">A text that the message of the exception must contain, or <c>null</c> to skip this check.</param>
    private Task AssertInvocationRefusedAsync(
        string callSiteText,
        string targetType,
        string targetMethod,
        CallSiteReceiverMode receiverMode,
        Func<ScriptContext, InvocationRedirectionRequest, InvocationRedirectionRequest>? customize = null,
        string? expectedMessage = null )
        => this.ExecuteAsync(
            s =>
            {
                var request = new InvocationRedirectionRequest( s.Invocation( callSiteText ), s.Target( targetType, targetMethod ), receiverMode );

                if ( customize != null )
                {
                    request = customize( s, request );
                }

                var exception = Assert.Throws<ArgumentException>( () => s.Factory.RedirectInvocation( s.Origin, request ) );
                Assert.False( s.Factory.IsRedirected( request.CallSite ) );

                if ( expectedMessage != null )
                {
                    Assert.Contains( expectedMessage, exception.Message, StringComparison.Ordinal );
                }
            } );

    /// <summary>
    /// Runs the compile-time pipeline on <see cref="_code"/> with <see cref="ScriptedExtension"/>, which runs the given script in its transforming
    /// hook, and asserts that the pipeline succeeds and that the script has run.
    /// </summary>
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
        /// <summary>
        /// Initializes a new instance of the <see cref="Script"/> class.
        /// </summary>
        public Script( Action<ScriptContext> action )
        {
            this.Action = action;
        }

        /// <summary>
        /// Gets the script.
        /// </summary>
        public Action<ScriptContext> Action { get; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="ScriptedExtension"/> has run the script.
        /// </summary>
        public bool HasRun { get; set; }
    }

    /// <summary>
    /// The objects that a script uses, with helpers that find the nodes of the test code.
    /// </summary>
    private sealed record ScriptContext( ExtensionTransformationContext Context, ExtensionTransformationFactory Factory, ExtensionContributionOrigin Origin )
    {
        /// <summary>
        /// Gets the compilation that results from all aspects of the stage.
        /// </summary>
        public CompilationModel Compilation => this.Context.StageFinalCompilation;

        /// <summary>
        /// Gets the single invocation that has the given text.
        /// </summary>
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

        /// <summary>
        /// Gets the single node of type <typeparamref name="T"/> that has the given text and satisfies the given predicate.
        /// </summary>
        /// <param name="text">The text of the node, or <c>null</c> to match any text.</param>
        /// <param name="predicate">An additional condition, or <c>null</c>.</param>
        public T Node<T>( string? text, Func<T, bool>? predicate = null )
            where T : SyntaxNode
            => this.Compilation.PartialCompilation.SyntaxTreeCollection
                .SelectMany( t => t.GetRoot().DescendantNodes() )
                .OfType<T>()
                .Single( n => (text == null || n.ToString() == text) && (predicate == null || predicate( n )) );

        /// <summary>
        /// Creates a redirection target from the single method of the given name, in the single type of the given name, that satisfies the given
        /// predicate.
        /// </summary>
        public CallSiteRedirectionTarget Target( string typeName, string methodName, Func<IMethod, bool>? predicate = null )
            => CallSiteRedirectionTarget.Existing(
                this.Compilation.Types.OfName( typeName ).Single().Methods.OfName( methodName ).Single( m => predicate == null || predicate( m ) ) );
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
                aspectInstance,
                0 );

            script.Action( new ScriptContext( context, context.TransformationFactory, origin ) );
            script.HasRun = true;

            return Task.CompletedTask;
        }
    }
}
