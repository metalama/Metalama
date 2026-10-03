// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_InstanceReceiver_Refused;

// A method group of an instance method has a receiver, which the receiver mode Drop would not evaluate. The factory refuses the request.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Greet",
            ( (INamedType) TypeFactory.GetType( typeof(Interceptors) ) ).Methods.OfName( "Greet" ).Single(),
            new TestRedirectionOptions { MethodReferences = true } );
}

internal class Greeter
{
    public void Greet() => Console.WriteLine( "hello" );
}

internal static class Interceptors
{
    public static void Greet() => Console.WriteLine( "intercepted hello" );
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain()
    {
        Action action = new Greeter().Greet;
        action();
    }
}
