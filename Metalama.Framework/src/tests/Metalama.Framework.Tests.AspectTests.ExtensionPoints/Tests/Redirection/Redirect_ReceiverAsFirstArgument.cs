// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_ReceiverAsFirstArgument;

// A call to an instance method is redirected to a static method that receives the receiver as its first argument. An implicit receiver is
// passed as this.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Greet",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Greet" ).Single(),
            new TestRedirectionOptions { ReceiverMode = "FirstArgument" } );
}

internal class Greeter
{
    public string Name = "greeter";

    public void Greet( string s ) => Console.WriteLine( $"{this.Name}: {s}" );
}

internal static class Interceptors
{
    public static void Greet( Greeter greeter, string s ) => Console.WriteLine( $"intercepted {greeter.Name}: {s}" );
}

// <target>
[Redirect]
internal class Program : Greeter
{
    public static void TestMain()
    {
        var greeter = new Greeter();
        greeter.Greet( "explicit" );

        new Program { Name = "program" }.Run();
    }

    private void Run() => Greet( "implicit" );
}
