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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.GenericMethod_RunTimeTypeParameter;

// The template has a run-time type parameter, which binds to the type parameter of the declared method. The declared method is generic, so
// the calls with two type arguments share it.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate( "Echo", nameof(this.Intercept), new TestTemplateRedirectionOptions { Placement = "Caller", Generic = true } );

    [Template]
    public T Intercept<T>( T value )
    {
        Console.WriteLine( $"Echoing a {typeof(T).Name}." );

        return meta.Proceed()!;
    }
}

internal static class Source
{
    public static T Echo<T>( T value ) => value;
}

// <target>
[Redirect]
internal class Program
{
    public static void TestMain()
    {
        Console.WriteLine( Source.Echo( 1 ) );
        Console.WriteLine( Source.Echo( "a" ) );
    }
}
