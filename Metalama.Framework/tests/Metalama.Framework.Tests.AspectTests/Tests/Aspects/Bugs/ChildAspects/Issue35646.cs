using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.Integration.Tests.Aspects.Subtemplates.Issue35646;

public class ParentAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Outbound.RequireAspect<ChildAspect>();
    }

    [Template]
    public void SomeTemplate()
    {
        Console.WriteLine( "Some template" );
    }
}

public class ChildAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var parentAspect = (ParentAspect)( (IAspectInstance)builder.AspectInstance.Predecessors[0].Instance ).Aspect;

        base.BuildAspect( builder );
        builder.WithTemplateProvider( parentAspect ).Override( nameof(parentAspect.SomeTemplate) );
    }
}

// <target>
public class C
{
    [ParentAspect]
    public void M() { }
}