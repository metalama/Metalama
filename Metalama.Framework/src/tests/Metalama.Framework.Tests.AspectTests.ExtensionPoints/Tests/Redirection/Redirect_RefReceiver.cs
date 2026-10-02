// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_RefReceiver;

// A call to a method of a mutable struct is redirected to a static method that receives the receiver by reference, so the mutation done by the
// interceptor is visible to the caller.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Increment",
            ( (INamedType) TypeFactory.GetType( typeof(Interceptors) ) ).Methods.OfName( "Increment" ).Single(),
            new TestRedirectionOptions { ReceiverMode = "FirstArgumentByRef" } );
}

internal struct Counter
{
    public int Value;

    public void Increment() => this.Value++;
}

internal static class Interceptors
{
    public static void Increment( ref Counter counter ) => counter.Value += 10;
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain()
    {
        var counter = new Counter();
        counter.Increment();
        Console.WriteLine( counter.Value );
    }
}
