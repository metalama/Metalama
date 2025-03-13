// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @FormatCompileTimeCode(false)
#endif

#pragma warning disable CS1998

using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LocalFunctions.Async;

public class RetryAttribute : OverrideMethodAspect
{
    // Template for non-async methods.
    public override dynamic? OverrideMethod()
    {
        return meta.Proceed();
    }

    // Template for async methods.
    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        async Task<object?> ExecuteCoreAsync()
        {
            return await meta.ProceedAsync();
        }

        return await Task.Run( ExecuteCoreAsync );
    }
}

// <target>
internal class C
{
    [Retry]
    private void Foo() { }

    [Retry]
    private async Task FooAsync() { }
}