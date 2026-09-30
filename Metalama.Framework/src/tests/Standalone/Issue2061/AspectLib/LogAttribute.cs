using Metalama.Framework.Aspects;
using System;

namespace AspectLib;

public class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Entering {meta.Target.Method}." );

        return meta.Proceed();
    }
}
