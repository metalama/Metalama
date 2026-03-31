using Metalama.Framework.Aspects;
using System;

namespace TestApp;

public class ReturnZeroAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        // Ignore the original implementation and always return 0
        Console.WriteLine("ReturnZeroAspect applied: Overriding method to return 0.");
        return 0;
    }
}
