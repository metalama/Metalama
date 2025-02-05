using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33446;

// <target>
internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return GetNumbers();

        IEnumerable<int> GetNumbers()
        {
            yield return 42;
        }
    }
}

// <target>
public class Target
{
    [Aspect]
    public IEnumerable<int> Foo()
    {
        return Array.Empty<int>();
    }
}