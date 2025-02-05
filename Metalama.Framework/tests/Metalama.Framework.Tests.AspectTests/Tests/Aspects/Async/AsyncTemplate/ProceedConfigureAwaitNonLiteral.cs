using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.ProceedConfigureAwaitNonLiteral;

public sealed class TransactionalMethodAttribute : OverrideMethodAspect
{
    [Introduce( WhenExists = OverrideStrategy.Ignore )]
    private bool _continueOnCapturedContext = false;

    public override dynamic? OverrideMethod() => throw new NotSupportedException();

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        var result = await meta.ProceedAsync().ConfigureAwait( _continueOnCapturedContext );
        await meta.This.OnTransactionMethodSuccessAsync();

        return result;
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