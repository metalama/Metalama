using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.RecordStruct_Manual;

/*
 * The record struct with a custom serializer.
 */

//<target>
public class TargetClass : BaseClass
{
    public override void Foo()
    {
        Console.WriteLine("Original");
    }
}