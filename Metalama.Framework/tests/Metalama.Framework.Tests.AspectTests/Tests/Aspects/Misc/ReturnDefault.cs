#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
#endif

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.ReturnDefault;

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
            var asyncInfo = meta.Target.Method.GetAsyncInfo();
            var returnType = asyncInfo.IsAsync == true ? asyncInfo.ResultType : meta.Target.Method.ReturnType;

            return meta.Default( returnType );
        }
    }
}

// <target>
public class TestClass
{
    [IgnoreException]
    public void VoidMethod()
    {
        throw new InvalidOperationException();
    }

    [IgnoreException]
    public async void AsyncVoidMethod()
    {
        await Task.Yield();

        throw new InvalidOperationException();
    }

    [IgnoreException]
    public int IntMethod()
    {
        throw new InvalidOperationException();
    }

    [IgnoreException]
    public Task TaskMethod()
    {
        throw new InvalidOperationException();
    }

    [IgnoreException]
    public async Task AsyncTaskMethod()
    {
        await Task.Yield();

        throw new InvalidOperationException();
    }

    [IgnoreException]
    public Task<int> TaskIntMethod()
    {
        throw new InvalidOperationException();
    }

    [IgnoreException]
    public async Task<int> AsyncTaskIntMethod()
    {
        await Task.Yield();

        throw new InvalidOperationException();
    }

    [IgnoreException]
    public ValueTask ValueTaskMethod()
    {
        throw new InvalidOperationException();
    }

    [IgnoreException]
    public async ValueTask AsyncValueTaskMethod()
    {
        await Task.Yield();

        throw new InvalidOperationException();
    }

    [IgnoreException]
    public ValueTask<int> ValueTaskIntMethod()
    {
        throw new InvalidOperationException();
    }

    [IgnoreException]
    public async ValueTask<int> AsyncValueTaskIntMethod()
    {
        await Task.Yield();

        throw new InvalidOperationException();
    }
}