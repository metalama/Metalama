using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue31113;

public class MyAspect : TypeAspect
{
    [Introduce]
    public void Method()
    {
        var method = meta.Target.Type.Methods.Single();
        method.Invoke();
    }
}

[MyAspect]
internal class C
{
    private void M() { }
}