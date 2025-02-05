#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

#if NET5_0_OR_GREATER
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Metalama.Framework;
using Metalama.Testing.AspectTesting;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncIterators.ProceedConfigureAwait;

class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => throw new NotSupportedException();

    public override async IAsyncEnumerable<dynamic?> OverrideAsyncEnumerableMethod()
    {
        Console.WriteLine($"Starting {meta.Target.Method.Name}");
        await foreach ( var item in meta.ProceedAsyncEnumerable().ConfigureAwait(false) )
        {
            Console.WriteLine($" Intercepting {item}");
            yield return item;
        }
        Console.WriteLine($"Ending {meta.Target.Method.Name}");
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
        Console.WriteLine("Yield 2");
        yield return 2;
        Console.WriteLine("Yield 3");
        yield return 3;
    }
}

#endif