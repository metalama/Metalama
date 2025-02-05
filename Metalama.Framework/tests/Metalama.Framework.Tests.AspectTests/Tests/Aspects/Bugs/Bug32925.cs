using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32925;

public class TheAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var result = meta.Proceed();

        // We invoke an extension method, but as a plain method.
        ExtensionClass.ExtensionMethod( result );

        return result;
    }
}

public static class ExtensionClass
{
    public static void ExtensionMethod( this object o ) { }
}

// <target>
public class C
{
    [TheAspect]
    public void M() { }
}