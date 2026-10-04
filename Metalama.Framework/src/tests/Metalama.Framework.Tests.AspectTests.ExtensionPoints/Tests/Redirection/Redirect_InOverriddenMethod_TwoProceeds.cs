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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InOverriddenMethod_TwoProceeds;

// The template calls meta.Proceed() twice, so the redirected call of the source body is executed twice.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Methods.OfName( "M" ).Single() ).Override( nameof(this.Template) );

        builder.TestRedirectCalls(
            "Compute",
            ( (INamedType) TypeFactory.GetType( typeof(Interceptors) ) ).Methods.OfName( "Compute" ).Single() );
    }

    [Template]
    public dynamic? Template()
    {
        meta.Proceed();

        return meta.Proceed();
    }
}

internal static class Source
{
    public static int Compute( int x ) => x + 1;
}

internal static class Interceptors
{
    public static int Compute( int x )
    {
        Console.WriteLine( $"intercepted {x}" );

        return x * 10;
    }
}

// <target>
[Redirect]
internal class Program
{
    public static void TestMain() => Console.WriteLine( new Program().M() );

    private int M() => Source.Compute( 2 );
}
