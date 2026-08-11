using Metalama.Framework.Aspects;
using System;

namespace TestApp;

/// <summary>
/// A trivial aspect that overrides the target method to return zero.
/// </summary>
public class ReturnZeroAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "ReturnZeroAspect applied: Overriding method to return 0." );

        return 0;
    }
}
