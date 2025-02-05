using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.GetOnlyAutopropertyOverride;

public sealed class TestAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get
        {
            Console.WriteLine( "getter" );

            return meta.Proceed();
        }
        set
        {
            Console.WriteLine( "setter" );

            meta.Proceed();
        }
    }
}

// <target>
public class Target
{
    [TestAspect]
    public int Test { get; }
}