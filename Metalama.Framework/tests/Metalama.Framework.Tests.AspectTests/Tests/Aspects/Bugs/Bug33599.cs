using System.IO;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33599;

public class Test1Attribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.Proceed();

        return default;
    }
}

public class Test2Attribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        _ = meta.Proceed();

        return default;
    }
}

// <target>
internal class Target
{
    [Test1]
    public MemoryStream M1()
    {
        return new MemoryStream();
    }

    [Test2]
    public MemoryStream M2()
    {
        return new MemoryStream();
    }

    [Test1]
    public MemoryStream M3() => new();

    [Test2]
    public MemoryStream M4() => new();
}