using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace TestApp;

public class ReturnZeroAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine("ReturnZeroAspect applied: Overriding method to return 0.");
        return 0;
    }
}
