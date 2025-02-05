using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.Simple_Static;

/*
 * Tests OverrideConstructor advice with a trivial template on a static constructor.
 */

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.StaticConstructor! ).Override( nameof(Template) );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "This is the override." );
        meta.Proceed();
    }
}

// <target>
[Override]
public class TargetClass
{
    static TargetClass()
    {
        Console.WriteLine( "This is the original static constructor." );
    }
}