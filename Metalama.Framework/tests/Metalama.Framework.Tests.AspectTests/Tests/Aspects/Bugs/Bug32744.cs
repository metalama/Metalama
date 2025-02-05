namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32744;

#pragma warning disable CS8321
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

public class TestAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return meta.Proceed();

        void Foo( int i ) { }
    }
}

internal class C
{
    // <target>
    [Test]
    private static int Bar()
    {
        return 42;
    }
}