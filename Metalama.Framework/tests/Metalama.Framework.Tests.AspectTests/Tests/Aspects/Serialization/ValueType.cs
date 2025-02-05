using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.ValueType;

/*
 * The serializable value type.
 */

//<target>
public class TargetClass : BaseClass
{
    public override void Foo()
    {
        Console.WriteLine("Original");
    }
}