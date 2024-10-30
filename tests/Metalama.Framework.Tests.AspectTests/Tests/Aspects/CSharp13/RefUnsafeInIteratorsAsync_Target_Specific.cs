#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
// @RequiredConstant(NET5_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.RefUnsafeInIteratorsAsync_Target_Specific;

class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        throw new NotImplementedException();
    }

    public async override Task<dynamic?> OverrideAsyncMethod()
    {
        Console.WriteLine($"Entering {meta.Target.Method}.");

        try
        {
            var result = await meta.ProceedAsync();

            Console.WriteLine($"{meta.Target.Method} succeeded with result {result}.");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{meta.Target.Method} failed with exception {ex}.");

            throw;
        }
    }

    public override IEnumerable<dynamic?> OverrideEnumerableMethod()
    {
        Console.WriteLine($"Entering {meta.Target.Method}.");

        foreach (var item in meta.ProceedEnumerable())
        {
            Console.WriteLine($"{meta.Target.Method} yielded {item}.");
            yield return item;
        }
    }

#if NET5_0_OR_GREATER
    public override async IAsyncEnumerable<dynamic?> OverrideAsyncEnumerableMethod()
    {
        Console.WriteLine($"Entering {meta.Target.Method}.");

        await foreach (var item in meta.ProceedAsyncEnumerable())
        {
            Console.WriteLine($"{meta.Target.Method} yielded {item}.");
            yield return item;
        }
    }

#endif
}

#if ROSLYN_4_12_0_OR_GREATER

// <target>
class Target
{
    [TheAspect]
    private async Task Async()
    {
        await Task.Yield();

        // unsafe
        unsafe
        {
            fixed (int* p = new int[1])
            {
            }
        }

        // ref
        ref int r = ref (new int[1])[0];

        // ref struct
        Span<int> s = stackalloc int[1];

        await Task.Yield();
    }

    [TheAspect]
    private IEnumerable<int> Iterator()
    {
        yield return 1;

        // unsafe
        unsafe
        {
            fixed (int* p = new int[1])
            {
            }
        }

        // ref
        ref int r = ref (new int[1])[0];

        // ref struct
        Span<int> s = stackalloc int[1];

        yield return 2;
    }

    [TheAspect]
    private async IAsyncEnumerable<int> AsyncIterator()
    {
        await Task.Yield();
        yield return 1;

        // unsafe
        unsafe
        {
            fixed (int* p = new int[1])
            {
            }
        }

        // ref
        ref int r = ref (new int[1])[0];

        // ref struct
        Span<int> s = stackalloc int[1];

        await Task.Yield();
        yield return 2;
    }
}

#endif