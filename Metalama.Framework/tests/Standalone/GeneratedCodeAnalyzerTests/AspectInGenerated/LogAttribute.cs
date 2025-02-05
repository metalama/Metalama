using Metalama.Framework.Aspects;

class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine("logged");

        return meta.Proceed();
    }
}