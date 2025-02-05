using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.CrossAssembly_PrivateTemplate;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        foreach (var method in builder.Target.Methods)
        {
            builder.With( method ).Override( nameof(Template) );
        }
    }

    [Template]
    private dynamic? Template()
    {
        Console.WriteLine( "Overridden" );

        return meta.Proceed();
    }
}