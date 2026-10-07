// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @MainMethod(TestMain)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_HiddenIntroducedParameter;

// The aspect introduces the parameter 'id' into the constructor, and the new call passes it. The introduced parameter has no symbol in the source
// compilation, but the lambda parameter 'id' of the source code hides it at the call site in the lambda, so the linker renames the lambda parameter
// and reports LAMA0661.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Constructors.Single() ).IntroduceParameter( "id", typeof(int), TypedConstant.Create( 7 ) );

        builder.TestRedirectCalls(
            "Log",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Log" ).Single(),
            new TestRedirectionOptions { Arguments = "message=argument:0; requestId=parameter:1" } );
    }
}

internal static class Source
{
    public static void Log( string message ) => Console.WriteLine( message );
}

internal static class Interceptors
{
    public static void Log( string message, int requestId ) => Console.WriteLine( $"{message} (request {requestId})" );
}

// <target>
[Redirect]
internal class Program
{
    public Program( int[] items )
    {
        Source.Log( "start" );
        Array.ForEach( items, id => Source.Log( $"item {id}" ) );
    }

    public static void TestMain() => _ = new Program( new[] { 1, 2 } );
}
