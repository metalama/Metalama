// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_ReceiverAsFirstArgument_Fabric;

// A project fabric redirects a call to an instance method to a static method that receives the receiver as its first argument.

internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
        => amender.SelectTypes()
            .Where( t => t.Name == "Program" )
            .TestRedirectCalls(
                "Greet",
                "Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_ReceiverAsFirstArgument_Fabric.Interceptors",
                "Greet",
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
