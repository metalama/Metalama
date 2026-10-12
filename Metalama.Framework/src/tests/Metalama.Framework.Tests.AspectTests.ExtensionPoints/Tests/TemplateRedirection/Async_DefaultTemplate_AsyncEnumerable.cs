// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
// @MainMethod(TestMain)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Async_DefaultTemplate_AsyncEnumerable;

// The source methods return an async enumerable or an async enumerator, and the selector uses the async template for any awaitable method.
// The default template is interpreted as an async iterator template, so the declared methods are async iterators that buffer meta.Proceed(),
// and the template code after meta.Proceed() runs after the source has been enumerated.

internal class RedirectAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var options = new TestTemplateRedirectionOptions { Placement = "Caller", UseAsyncTemplateForAnyAwaitable = true };

        builder.TestRedirectCallsToTemplate( "GetEnumerable", nameof(this.Intercept), options );
        builder.TestRedirectCallsToTemplate( "GetEnumerator", nameof(this.Intercept), options );
    }

    [Template]
    public dynamic? Intercept()
    {
        Console.WriteLine( "Before." );
        var result = meta.Proceed();
        Console.WriteLine( "After." );

        return result;
    }
}

internal static class Source
{
    public static async IAsyncEnumerable<int> GetEnumerable()
    {
        await Task.Yield();
        Console.WriteLine( "Yield 1." );

        yield return 1;

        await Task.Yield();
        Console.WriteLine( "Yield 2." );

        yield return 2;
    }

    public static async IAsyncEnumerator<int> GetEnumerator()
    {
        await Task.Yield();
        Console.WriteLine( "Yield 1." );

        yield return 1;

        await Task.Yield();
        Console.WriteLine( "Yield 2." );

        yield return 2;
    }
}

// <target>
[Redirect]
internal class Program
{
    public static async Task TestMain()
    {
        await foreach (var i in Source.GetEnumerable())
        {
            Console.WriteLine( $"Received {i}." );
        }

        await using (var enumerator = Source.GetEnumerator())
        {
            while (await enumerator.MoveNextAsync())
            {
                Console.WriteLine( $"Received {enumerator.Current}." );
            }
        }
    }
}
