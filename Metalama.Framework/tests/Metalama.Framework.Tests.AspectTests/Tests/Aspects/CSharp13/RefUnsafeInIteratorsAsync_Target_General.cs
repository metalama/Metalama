#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.RefUnsafeInIteratorsAsync_Target_General;

class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine($"Entering {meta.Target.Method}.");

        try
        {
            var result = meta.Proceed();

            Console.WriteLine($"{meta.Target.Method} succeeded with result {result}.");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{meta.Target.Method} failed with exception {ex}.");

            throw;
        }
    }
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

    // async iterator would generate invalid code, see #31108
}

#endif