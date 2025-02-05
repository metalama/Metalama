using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.RecordClass_Manual;

/*
 * The record class with a custom serializer.
 */

//<target>
public class TargetClass : BaseClass
{
    public override void Foo()
    {
        Console.WriteLine("Original");
    }
}