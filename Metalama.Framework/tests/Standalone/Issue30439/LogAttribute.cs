using Metalama.Framework.Aspects;
using System;

public class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Executing {meta.Target.Method.ToDisplayString()}" );
        return meta.Proceed();
    }
}