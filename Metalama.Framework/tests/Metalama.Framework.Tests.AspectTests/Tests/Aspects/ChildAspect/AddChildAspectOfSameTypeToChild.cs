using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.AddChildAspectOfSameTypeToChild;

[AttributeUsage( AttributeTargets.Class )]
public class MyAspect : OverrideMethodAspect, IAspect<INamedType>
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Oops" );

        return meta.Proceed();
    }

    public void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Outbound.SelectMany( t => t.Methods ).AddAspectIfEligible( _ => this );
    }

    public void BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }
}

// <target>
[MyAspect]
internal class C
{
    private void M() { }
}