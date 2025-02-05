#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.RefInIteratorsAsync_Template;

#if ROSLYN_4_12_0_OR_GREATER

class TheAspect : TypeAspect
{
    // Unsafe is not allowed in templates (LAMA0101), ref and ref structs are.

    [Introduce]
    private async Task Async()
    {
        await Task.Yield();

        // ref
        ref int r = ref (new int[1])[0];

        // ref struct
        Span<int> s = stackalloc int[1];

        await Task.Yield();
    }

    [Introduce]
    private IEnumerable<int> Iterator()
    {
        yield return 1;

        // ref
        ref int r = ref (new int[1])[0];

        // ref struct
        Span<int> s = stackalloc int[1];

        yield return 2;
    }

    [Introduce]
    private async IAsyncEnumerable<int> AsyncIterator()
    {
        await Task.Yield();
        yield return 1;

        // ref
        ref int r = ref (new int[1])[0];

        // ref struct
        Span<int> s = stackalloc int[1];

        await Task.Yield();
        yield return 2;
    }
}

// <target>
[TheAspect]
class Target
{
}

#endif