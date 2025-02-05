using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.UseEnumerableTemplateForAnyEnumerable;

public class AspectAttribute : OverrideMethodAspect
{
    public AspectAttribute()
    {
        UseEnumerableTemplateForAnyEnumerable = true;
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "default" );

        return meta.Proceed();
    }

    public override IEnumerable<dynamic?> OverrideEnumerableMethod()
    {
        Console.WriteLine( "enumerable" );

        return meta.ProceedEnumerable();
    }
}

// <target>
internal class EmptyOverrideFieldOrPropertyExample
{
    [Aspect]
    private IEnumerable<int> M()
    {
        return new[] { 42 };
    }
}