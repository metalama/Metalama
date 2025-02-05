using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32493;

//<target>
public class TargetClass : BaseClass
{
    public override void Foo()
    {
        Console.WriteLine("Original");
    }
}