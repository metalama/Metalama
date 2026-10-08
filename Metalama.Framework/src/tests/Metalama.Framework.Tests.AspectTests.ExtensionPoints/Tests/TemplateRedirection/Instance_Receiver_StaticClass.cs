// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Instance_Receiver_StaticClass;

// The calls to an instance method are redirected to a static method whose first parameter receives the receiver of the call. The run-time
// parameter of the template binds to the parameter of the source method by its position, after the receiver parameter.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => builder.TestRedirectCallsToTemplate( "Greet", nameof(this.Intercept) );

    [Template]
    public dynamic? Intercept( string text )
    {
        Console.WriteLine( $"Greeting '{text}'." );

        return meta.Proceed();
    }
}

internal class Greeter
{
    public string Prefix { get; set; } = "Hello";

    public string Greet( string name ) => $"{this.Prefix}, {name}!";
}

// <target>
[Redirect]
internal class Program
{
    public static void TestMain()
    {
        var greeter = new Greeter();
        Console.WriteLine( greeter.Greet( "world" ) );
    }
}
