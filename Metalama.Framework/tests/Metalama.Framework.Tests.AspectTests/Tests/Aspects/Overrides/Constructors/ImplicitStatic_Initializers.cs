using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.ImplicitStatic_Initializers;

/*
 * Tests single static OverrideConstructor advice with existing field/property initializers.
 */

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Constructors.Single() ).Override( nameof(Template) );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( $"This is the override start." );
        meta.Proceed();
        Console.WriteLine( $"This is the override end." );
    }
}

// <target>
[Override]
public class TargetClass
{
    public static int F = 42;

    public static int P { get; } = 42;
}