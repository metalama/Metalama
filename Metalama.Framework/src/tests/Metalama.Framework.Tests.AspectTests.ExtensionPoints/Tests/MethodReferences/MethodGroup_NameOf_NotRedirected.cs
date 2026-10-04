// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_NameOf_NotRedirected;

// The name of a method in a nameof expression is not a method reference, so it is not redirected.

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
    public static void TestMain() => Console.WriteLine( nameof(Source.Compute) );
}
