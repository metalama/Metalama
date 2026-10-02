// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InOverriddenMethod_Inlined;

// A call in the body of a method that an aspect overrides is redirected in the inlined source body. The call introduced by the template is not
// redirected, because it is not in the source compilation.

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
        Console.WriteLine( Source.Compute( 1 ) );

        return meta.Proceed();
    }
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
    public static void TestMain() => new Program().M();

    private void M()
    {
        Console.WriteLine( Source.Compute( 2 ) );
    }
}
