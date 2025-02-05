using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33163;

public sealed class TestAspectAttribute : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get => 42;
        set
        {
            meta.Proceed();
            meta.Proceed();
        }
    }
}

// <target>
public class TestClass
{
    [TestAspect]
    public int PrivateSetter
    {
        get
        {
            return 42;
        }

        private set { }
    }

    [TestAspect]
    public int PrivateSetter_Auto { get; private set; }

    [TestAspect]
    public int PrivateGetter
    {
        private get
        {
            return 42;
        }

        set { }
    }

    [TestAspect]
    public int PrivateGetter_Auto { private get; set; }
}