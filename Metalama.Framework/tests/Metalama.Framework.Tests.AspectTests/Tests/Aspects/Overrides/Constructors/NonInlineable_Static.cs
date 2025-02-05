using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.NonInlineable_Static;

/*
 * Tests that single static OverrideConstructor advice with non-inlineable template produces a diagnostic error.
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
        meta.Proceed();
    }
}

// <target>
[Override]
internal class TargetClass
{
    static TargetClass()
    {
        Console.WriteLine( $"This is the original constructor." );
    }
}