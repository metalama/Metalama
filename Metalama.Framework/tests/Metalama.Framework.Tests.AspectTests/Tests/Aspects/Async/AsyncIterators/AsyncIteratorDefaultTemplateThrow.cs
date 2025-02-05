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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncIterators.AsyncIteratorDefaultTemplateThrow;

class Aspect : MethodAspect
{
    public override void BuildAspect(IAspectBuilder<IMethod> builder)
    {
        base.BuildAspect(builder);

        builder.Override( nameof(OverrideMethod));
    }

    [Template]
    public async Task<dynamic?> OverrideMethod()
    {
        await Task.Yield();
        throw new Exception();
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