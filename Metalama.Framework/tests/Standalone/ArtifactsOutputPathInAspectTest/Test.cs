using Metalama.Framework.Aspects;
using System;

namespace ArtifactsOutputPathInAspectTest;

public class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "overridden" );

        return meta.Proceed();
    }
}

internal class Target
{
    // <target>
    [TestAspect]
    public static void M()
    {
    }
}