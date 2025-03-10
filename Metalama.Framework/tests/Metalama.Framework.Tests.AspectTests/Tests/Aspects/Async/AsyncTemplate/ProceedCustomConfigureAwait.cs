// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncTemplate.ProceedCustomConfigureAwait;

public sealed class TransactionalMethodAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => throw new NotSupportedException();

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        var result = await meta.ProceedAsync().NoContext();
        await meta.This.OnTransactionMethodSuccessAsync();

        return result;
    }
}

internal static class TaskExtensions
{
    public static ConfiguredTaskAwaitable NoContext( this Task task ) => task.ConfigureAwait( false );

    public static ConfiguredTaskAwaitable<T> NoContext<T>( this Task<T> task ) => task.ConfigureAwait( false );
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