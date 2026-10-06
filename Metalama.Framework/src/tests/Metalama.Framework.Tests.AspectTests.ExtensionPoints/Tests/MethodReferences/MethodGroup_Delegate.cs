// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_Delegate;

// Method groups converted to a delegate, directly or in an explicit delegate creation, are redirected. The invocation of the same method is
// not redirected, because the request is for method references.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls(
            "Compute",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Compute" ).Single(),
            new TestRedirectionOptions { MethodReferences = true } );
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
internal static class Program
{
    public static void TestMain()
    {
        Func<int, int> converted = Source.Compute;
        var created = new Func<int, int>( Source.Compute );
        Console.WriteLine( $"{converted( 1 )} {created( 2 )} {Source.Compute( 3 )}" );
    }
}
