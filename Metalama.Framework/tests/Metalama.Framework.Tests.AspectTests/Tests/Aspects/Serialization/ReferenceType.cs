using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.ReferenceType;

/*
 * The serializable reference type.
 */

//<target>
public class TargetClass : BaseClass
{
    public override void Foo()
    {
        Console.WriteLine("Original");
    }
}