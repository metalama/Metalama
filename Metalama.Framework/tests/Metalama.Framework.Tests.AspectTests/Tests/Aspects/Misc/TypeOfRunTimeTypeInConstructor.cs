using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.TypeOfRunTimeTypeInConstructor;

internal class TestAspect : TypeAspect
{
    public TestAspect()
    {
        _ = typeof(RunTimeClass);
    }
}

internal class RunTimeClass { }

// <target>
[TestAspect]
internal class TargetClass { }