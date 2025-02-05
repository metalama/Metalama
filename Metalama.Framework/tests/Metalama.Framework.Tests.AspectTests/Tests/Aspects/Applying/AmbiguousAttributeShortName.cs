using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Applying.AmbiguousAttributeShortName;

public class RequiresAttributeAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Applied." );

        return null;
    }
}

// <target>
internal class TargetClass
{
    [RequiresAttribute]
    private void ShortName() { }

    [RequiresAttributeAttribute]
    private void LongName() { }
}