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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Restrictions_ChangeLockedTypeParameter_Refused;

// The type parameter of the declared method is copied from the source method and locked. Renaming it throws, so the method is not declared
// and the call site is left unchanged.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate(
            "Echo",
            nameof(this.Intercept),
            new TestTemplateRedirectionOptions { Placement = "Caller", Generic = true, PrebuiltBuilder = true, RenameTypeParameter = "T=U" } );

    [Template]
    public T Intercept<T>( T value ) => meta.Proceed()!;
}

internal static class Source
{
    public static T Echo<T>( T value ) => value;
}

// <target>
[Redirect]
internal class Program
{
    public static void TestMain() => Console.WriteLine( Source.Echo( 1 ) );
}
