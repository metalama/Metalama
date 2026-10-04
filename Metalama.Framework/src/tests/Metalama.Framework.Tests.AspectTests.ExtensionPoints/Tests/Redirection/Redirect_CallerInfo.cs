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
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_CallerInfo;

// The interceptor declares a caller information parameter, which the compiler fills at the rewritten call site with the name of the member
// that contains the call.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.TestRedirectCalls(
            "Where",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Where" ).Single() );
    }
}

internal static class Source
{
    public static string Where( string s, [CallerMemberName] string member = "" ) => $"{s} {member}";
}

internal static class Interceptors
{
    public static string Where( string s, [CallerMemberName] string member = "" ) => $"intercepted {s} {member}";
}

// <target>
[Redirect]
internal static class Program
{
    public static void TestMain() => Console.WriteLine( Source.Where( "x" ) );
}
