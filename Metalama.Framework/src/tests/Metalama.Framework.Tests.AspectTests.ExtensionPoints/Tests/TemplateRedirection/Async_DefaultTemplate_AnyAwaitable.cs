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

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Async_DefaultTemplate_AnyAwaitable;

// The source methods return a task, and the selector uses the async template for any awaitable method. The default template is interpreted as an
// async template, so the declared methods are async and await meta.Proceed(), and the template code after meta.Proceed() runs after the task completes.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var options = new TestTemplateRedirectionOptions { Placement = "Caller", UseAsyncTemplateForAnyAwaitable = true };

        builder.TestRedirectCallsToTemplate( "ComputeAsync", nameof(this.Intercept), options );
        builder.TestRedirectCallsToTemplate( "RunAsync", nameof(this.Intercept), options );
    }

    [Template]
    public dynamic? Intercept()
    {
        try
        {
            return meta.Proceed();
        }
        finally
        {
            Console.WriteLine( "Done." );
        }
    }
}

internal static class Source
{
    public static Task<int> ComputeAsync( int x )
        => Task.Run(
            () =>
            {
                Console.WriteLine( "Computing." );

                return x * 2;
            } );

    public static async Task RunAsync()
    {
        await Task.Yield();
        Console.WriteLine( "Running." );
    }
}

// <target>
[Redirect]
internal class Program
{
    public static void TestMain()
    {
        Console.WriteLine( Source.ComputeAsync( 21 ).Result );
        Source.RunAsync().Wait();
    }
}
