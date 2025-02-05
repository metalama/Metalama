using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.NoSerializableFields_CrossAssembly;

/*
 * The serializable base class of a serializable type.
 */

//<target>
public class TargetClass : BaseClass
{
    public override void Foo()
    {
        Console.WriteLine("Original");
    }
}