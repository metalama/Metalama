#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

#if NET5_0_OR_GREATER
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Metalama.Framework;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncIterators.AsyncIteratorTemplateSkippedYield;

#pragma warning disable CS0162 // Unreachable code detected

class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        throw new NotSupportedException();
    }

    public override async IAsyncEnumerable<dynamic?> OverrideAsyncEnumerableMethod()
    {
        Console.WriteLine($"Starting {meta.Target.Method.Name}");

        if (true)
        {
            throw new Exception();
        }
        else
        {
            await foreach (var item in meta.ProceedAsyncEnumerable())
            {
                Console.WriteLine($" Intercepting {item}");
                yield return item;
            }
        }
    }

    public override async IAsyncEnumerator<dynamic?> OverrideAsyncEnumeratorMethod()
    {
        Console.WriteLine($"Starting {meta.Target.Method.Name}");
        var enumerator = meta.ProceedAsyncEnumerator();

        if (true)
        {
            throw new Exception();
        }
        else
        {
            while (await enumerator.MoveNextAsync())
            {
                Console.WriteLine($" Intercepting {enumerator.Current}");
                yield return enumerator.Current;
            }
        }
    }
}

// <target>
class TargetCode
{
    [Aspect]
    public async IAsyncEnumerable<int> Enumerable(int a)
    {
        await Task.Yield();
        Console.WriteLine("Yield 1");
        yield return 1;
    }
    
    [Aspect]
    public async IAsyncEnumerator<int> Enumerator(int a)
    {
        await Task.Yield();
        Console.WriteLine("Yield 1");
        yield return 1;
    }  
}

#endif