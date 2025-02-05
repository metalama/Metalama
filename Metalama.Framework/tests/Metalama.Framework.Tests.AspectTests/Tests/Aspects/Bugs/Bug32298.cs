using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32298;

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var field in builder.Target.ForCompilation( builder.Advice.MutableCompilation ).Fields)
        {
            builder.With( field ).Override( nameof(Template) );
        }
    }

    [Introduce]
    public int IntroducedField;

    [Template]
    public dynamic? Template
    {
        get
        {
            Console.WriteLine( "This is the overridden getter." );

            return meta.Proceed();
        }

        set
        {
            Console.WriteLine( "This is the overridden setter." );
            meta.Proceed();
        }
    }
}

// <target>
[Override]
public class C
{
    private void M() { }
}