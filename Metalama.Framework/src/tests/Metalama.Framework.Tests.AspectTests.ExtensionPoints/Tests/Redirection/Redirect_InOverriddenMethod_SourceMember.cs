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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InOverriddenMethod_SourceMember;

// The template calls meta.Proceed() in a local function, so the linker does not inline the source body and keeps it in a separate source member. The
// call in the source body is redirected there. The interceptor declares a caller member name parameter, and the linker substitutes the name
// of the overridden method for the name of the source member.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Methods.OfName( "M" ).Single() ).Override( nameof(this.Template) );

        builder.TestRedirectCalls(
            "Where",
            TypeFactory.GetNamedType( typeof(Interceptors) ).Methods.OfName( "Where" ).Single() );
    }

    [Template]
    public string Template()
    {
        string Proceed() => meta.Proceed()!;

        return Proceed();
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
internal class Program
{
    public static void TestMain() => Console.WriteLine( new Program().M() );

    private string M() => Source.Where( "x" );
}
