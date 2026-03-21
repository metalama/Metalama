// Standalone repro for issue #1414: Metalama fails when nuget.config
// contains a relative path in fallbackPackageFolders.

using System;
using Metalama.Framework.Aspects;

internal class Class1
{
    private int _counter;

    [Log]
    public void DoWork()
    {
        _counter++;
    }
}

internal class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Executing {meta.Target.Method.Name}" );

        return meta.Proceed();
    }
}
