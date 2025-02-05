using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.StructAspect;

internal struct Aspect : IAspect<IMethod>
{
    public void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override( nameof(OverrideMethod) );
    }

    public void BuildEligibility( IEligibilityBuilder<IMethod> builder ) { }

    [Template]
    private dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );

        return meta.Proceed();
    }
}