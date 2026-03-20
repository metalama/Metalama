// Standalone repro for issue #1414: Metalama fails when nuget.config
// contains a relative path in fallbackPackageFolders.

using Metalama.Framework.Aspects;

internal class Class1
{
    [Log]
    public void DoWork() { }
}

internal class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Executing {meta.Target.Method.Name}" );

        return meta.Proceed();
    }
}
