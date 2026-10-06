// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InPropertyAndEventInitializer;

// Calls in the initializer of an automatic property and of a field-like event are redirected.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Compute",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Compute" ).Single() );
}

internal static class Source
{
    public static int Compute( int x ) => x + 1;
}

internal static class Interceptors
{
    public static int Compute( int x ) => x * 10;
}

internal static class Handlers
{
    public static Action Create( int x ) => () => Console.WriteLine( $"handler {x}" );

    public static Action CreateIntercepted( int x ) => () => Console.WriteLine( $"intercepted handler {x}" );
}

// <target>
[Redirect]
internal class Program
{
    public int P { get; } = Source.Compute( 1 );

    public event Action E = Handlers.Create( Source.Compute( 2 ) );

    public static void TestMain()
    {
        var program = new Program();
        Console.WriteLine( program.P );
        program.E();
    }
}
