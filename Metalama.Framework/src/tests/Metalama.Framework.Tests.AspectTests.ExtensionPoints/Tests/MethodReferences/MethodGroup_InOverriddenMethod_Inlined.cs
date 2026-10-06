// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_InOverriddenMethod_Inlined;

// A method group in the body of a method that an aspect overrides is redirected in the inlined source body.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Methods.OfName( "M" ).Single() ).Override( nameof(this.Template) );

        builder.TestRedirectCalls(
            "Compute",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Compute" ).Single(),
            new TestRedirectionOptions { MethodReferences = true } );
    }

    [Template]
    public dynamic? Template() => meta.Proceed();
}

internal static class Source
{
    public static int Compute( int x ) => x + 1;
}

internal static class Interceptors
{
    public static int Compute( int x ) => x * 10;
}

// <target>
[Redirect]
internal class Program
{
    public static void TestMain() => Console.WriteLine( new Program().M()( 2 ) );

    private Func<int, int> M() => Source.Compute;
}
