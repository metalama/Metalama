#if TEST_OPTIONS
// @Skipped(#33758)
#endif

using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.ReferenceType_RuntimeInterfaceImpl;

/*
 * A serializable reference type implicitly implementing an interface.
 */

//<target>
public class TargetClass : BaseClass
{
    public override void Foo()
    {
        Console.WriteLine("Original");
    }
}