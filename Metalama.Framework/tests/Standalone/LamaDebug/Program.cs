using Metalama.Framework.Aspects;

internal class Program
{
    [Aspect]
    private static void Main()
    {
        Console.WriteLine( "from Main" );
    }
}

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "from aspect" );

        return meta.Proceed();
    }
}
