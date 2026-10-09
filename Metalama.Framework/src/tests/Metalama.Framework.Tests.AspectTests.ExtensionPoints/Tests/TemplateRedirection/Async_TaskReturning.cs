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
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Async_TaskReturning;

// The source method returns a task. The template is not async, so the declared method returns the task of the source method, and the template
// code runs before the task completes.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.TestRedirectCallsToTemplate( "ComputeAsync", nameof(this.Intercept), new TestTemplateRedirectionOptions { Placement = "Caller" } );

    [Template]
    public dynamic? Intercept()
    {
        Console.WriteLine( "Starting." );

        return meta.Proceed();
    }
}

internal static class Source
{
    public static async Task<int> ComputeAsync( int x )
    {
        await Task.Yield();

        return x * 2;
    }
}

// <target>
[Redirect]
internal class Program
{
    public static void TestMain() => Console.WriteLine( Source.ComputeAsync( 21 ).Result );
}
