// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
#endif

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.ReturnDefaultErrors;

internal sealed class IgnoreExceptionAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        try
        {
            return meta.Proceed();
        }
        catch
        {
            return meta.Default( meta.Target.Method.GetAsyncInfo().ResultType );
        }
    }
}

// <target>
public class TestClass
{
    [IgnoreException]
    public Task TaskMethod()
    {
        throw new InvalidOperationException();
    }

    [IgnoreException]
    public Task<int> TaskIntMethod()
    {
        throw new InvalidOperationException();
    }

    [IgnoreException]
    public ValueTask ValueTaskMethod()
    {
        throw new InvalidOperationException();
    }

    [IgnoreException]
    public ValueTask<int> ValueTaskIntMethod()
    {
        throw new InvalidOperationException();
    }

#if NET5_0_OR_GREATER
    [IgnoreException]
    public IAsyncEnumerable<int> IAsyncEnumerableMethod()
    {
        throw new InvalidOperationException();
    }

    [IgnoreException]
    public async IAsyncEnumerable<int> AsyncIAsyncEnumerableMethod()
    {
        await Task.Yield();
        yield return 42;
        throw new InvalidOperationException();
    }

#endif
}