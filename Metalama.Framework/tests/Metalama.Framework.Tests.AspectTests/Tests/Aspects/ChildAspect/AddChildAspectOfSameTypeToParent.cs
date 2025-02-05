using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.AddChildAspectOfSameTypeToParent;

[AttributeUsage( AttributeTargets.Method )]
public class MyAspect : Aspect, IAspect<IMethod>, IAspect<INamedType>
{
    public void BuildAspect( IAspectBuilder<INamedType> builder ) { }

    public void BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }

    public void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Outbound.Select( t => t.DeclaringType ).AddAspect( _ => this );
    }

    public void BuildEligibility( IEligibilityBuilder<IMethod> builder ) { }
}

// <target>

internal class C
{
    [MyAspect]
    private void M() { }
}