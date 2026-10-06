// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_InstanceReceiver_Refused;

// The method group 'new Greeter().Greet' refers to an instance method, so it has a receiver: the object created by 'new Greeter()', which the
// delegate binds. The receiver mode Drop means that the new method group does not pass the receiver to the target: the method group is replaced
// by the static method 'Interceptors.Greet', which has no parameter for it. The object would then be neither created nor used, so the factory
// refuses the request, and the source code is not changed. The receiver mode is written explicitly, although Drop is the default of the test
// extension.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Greet",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Greet" ).Single(),
            new TestRedirectionOptions { MethodReferences = true, ReceiverMode = "Drop" } );
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
