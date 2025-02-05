using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue31159;

public class BaseAspect : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        Console.WriteLine( "Valid." );
    }
}