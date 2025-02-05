using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.RecordClass_NonPositional;

/*
 * The non-positional record class.
 */

//<target>
public class TargetClass : BaseClass
{
    public override void Foo()
    {
        Console.WriteLine("Original");
    }
}