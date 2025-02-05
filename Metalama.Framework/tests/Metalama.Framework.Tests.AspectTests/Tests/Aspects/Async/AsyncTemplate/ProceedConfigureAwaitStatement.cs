using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.ProceedConfigureAwaitStatement;

public sealed class TransactionalMethodAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => throw new NotSupportedException();

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        await meta.ProceedAsync().ConfigureAwait( false );
        await meta.This.OnTransactionMethodSuccessAsync();

        return default;
    }
}

// <target>
public class TargetClass
{
    protected async Task OnTransactionMethodSuccessAsync()
    {
        await Task.Yield();
    }

    [TransactionalMethod]
    public async Task<int> DoSomethingAsync()
    {
        await Task.Yield();
        Console.WriteLine( "Hello" );

        return 42;
    }

    [TransactionalMethod]
    public async Task DoSomethingAsync2()
    {
        await Task.Yield();
        Console.WriteLine( "Hello" );
    }
}