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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_EventAddRemove;

// The method groups of an event subscription and of the matching unsubscription are both redirected, so the handler is removed.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "OnChanged",
            ( (INamedType) TypeFactory.GetType( typeof(Interceptors) ) ).Methods.OfName( "OnChanged" ).Single(),
            new TestRedirectionOptions { MethodReferences = true } );
}

internal static class Handlers
{
    public static void OnChanged() => Console.WriteLine( "source handler" );
}

internal static class Interceptors
{
    public static void OnChanged() => Console.WriteLine( "intercepted handler" );
}

// <target>
[Redirect]
internal static class Program
{
    private static event Action? Changed;

    public static void TestMain()
    {
        Changed += Handlers.OnChanged;
        Changed?.Invoke();
        Changed -= Handlers.OnChanged;
        Console.WriteLine( Changed == null );
    }
}
