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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_HiddenParameter;

// The new call passes the parameter 'id' of the calling member. Inside the lambdas, a lambda parameter and a local variable named 'id' hide it,
// so the linker renames them, with their references, and reports LAMA0661 for each of them. The member of the anonymous object keeps its name.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Log",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Log" ).Single(),
            new TestRedirectionOptions { Arguments = "message=argument:0; requestId=parameter:0" } );
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
internal static class Program
{
    private static void Handle( int id, int[] items )
    {
        Source.Log( "start" );

        Array.ForEach( items, id => Source.Log( $"item {id} {new { id }.id}" ) );

        Action end = () =>
        {
            var id = 99;
            Source.Log( $"end {id}" );
        };

        end();
    }

    public static void TestMain() => Handle( 7, new[] { 1, 2 } );
}
