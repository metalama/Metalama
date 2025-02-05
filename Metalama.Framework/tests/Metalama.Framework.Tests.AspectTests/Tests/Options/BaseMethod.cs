#if TEST_OPTIONS
// @Include(_Common.cs)
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.BaseMethod;

public class C
{
    [MyOptions( "C.Method" )]
    [ShowOptionsAspect]
    public virtual void Method() { }
}

public class D : C
{
    // <target>
    [ShowOptionsAspect]
    public override void Method() { }
}