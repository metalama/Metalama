// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_StaticMethod;

// The calls to Console.WriteLine( string ) in the target type are redirected to a static method of another type. The source method is static,
// so the receiver is dropped.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCalls( "WriteLine", TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Log" ).Single() );
}

internal static class Interceptors
{
    public static void Log( string s ) => Console.Out.WriteLine( $"intercepted: {s}" );
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain()
    {
        Console.WriteLine( "x" );
    }
}
