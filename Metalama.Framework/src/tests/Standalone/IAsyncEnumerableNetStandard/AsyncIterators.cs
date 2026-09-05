// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Regression test for #1963. When the OverrideMethod template is applied to an async iterator, the generated
// code buffers the stream by calling RunTimeAspectHelper.BufferAsync. That member must therefore exist in
// every asset of Metalama.Framework.Redist that a supported target framework can resolve, including the
// netstandard2.0 asset, which is also what a project targeting net8.0 or net9.0 resolves.

internal class BufferingAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Entering {meta.Target.Method.Name}." );

        return meta.Proceed();
    }
}

// The result of meta.Proceed() is stored in a local variable, which prevents the linker from inlining the call
// to the original method. This is the shape that fails in Metalama.Tests.NopCommerce.
internal class UninlineableBufferingAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var result = meta.Proceed();

        Console.WriteLine( $"Leaving {meta.Target.Method.Name}." );

        return result;
    }
}

internal class AsyncIteratorTarget
{
    [BufferingAspect]
    internal static async IAsyncEnumerable<int> GetValuesAsync()
    {
        await Task.Yield();

        yield return 1;
    }

    [UninlineableBufferingAspect]
    internal static async IAsyncEnumerable<int> GetUninlineableValuesAsync()
    {
        await Task.Yield();

        yield return 2;
    }

    [BufferingAspect]
    internal static async IAsyncEnumerator<int> GetValuesEnumeratorAsync()
    {
        await Task.Yield();

        yield return 3;
    }

    [UninlineableBufferingAspect]
    internal static async IAsyncEnumerator<int> GetUninlineableValuesEnumeratorAsync()
    {
        await Task.Yield();

        yield return 4;
    }
}
