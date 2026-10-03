// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_ArgumentOfRedirectedCall;

// A method group is an argument of a call that is also redirected. Both are rewritten.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var interceptors = (INamedType) TypeFactory.GetType( typeof(Interceptors) );

        builder.TestRedirectCalls( "Run", interceptors.Methods.OfName( "Run" ).Single() );
        builder.TestRedirectCalls( "Compute", interceptors.Methods.OfName( "Compute" ).Single(), new TestRedirectionOptions { MethodReferences = true } );
    }
}

internal static class Source
{
    public static int Compute( int x ) => x + 1;

    public static int Run( Func<int, int> function ) => function( 1 );
}

internal static class Interceptors
{
    public static int Compute( int x ) => x * 10;

    public static int Run( Func<int, int> function ) => function( 2 );
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain() => Console.WriteLine( Source.Run( Source.Compute ) );
}
