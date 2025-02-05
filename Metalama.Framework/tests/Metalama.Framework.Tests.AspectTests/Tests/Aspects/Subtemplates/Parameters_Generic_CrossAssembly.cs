using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Parameters_Generic_CrossAssembly;

public class Aspect : BaseAspect
{
    [Introduce]
    private int Add(int a)
    {
        NonOptional<int>(a, meta.CompileTime(1));
        Optional<int>();
        Optional<int>(a: a);
        Optional<int>(b: meta.CompileTime(1));
        Optional<int>(a: a, b: meta.CompileTime(1));

        meta.InvokeTemplate(nameof(NonOptional2), args: new { T = typeof(int), a = 1 });
        meta.InvokeTemplate(nameof(Optional2), args: new { T = typeof(int) });
        meta.InvokeTemplate(nameof(Optional2), args: new { T = typeof(int), a = 1 });
        meta.InvokeTemplate(nameof(Optional2), args: new { T = typeof(int), b = 1 });
        meta.InvokeTemplate(nameof(Optional2), args: new { T = typeof(int), a = 1, b = 1 });

        throw new Exception();
    }
}

// <target>
[Aspect]
public class TargetCode { }