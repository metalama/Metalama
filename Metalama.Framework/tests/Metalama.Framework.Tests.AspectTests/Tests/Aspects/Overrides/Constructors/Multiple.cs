using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.Multiple;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OuterOverrideAttribute), typeof(InnerOverrideAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.Multiple;

/*
 * Tests single OverrideConstructor advice used multiple times in multiple aspects.
 */

public class InnerOverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Constructors.Single() ).Override( nameof(Template), args: new { i = 1 } );
        builder.With( builder.Target.Constructors.Single() ).Override( nameof(Template), args: new { i = 2 } );
    }

    [Template]
    public void Template( [CompileTime] int i )
    {
        Console.WriteLine( $"This is the inner override {i}." );
        meta.Proceed();
    }
}

public class OuterOverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Constructors.Single() ).Override( nameof(Template), args: new { i = 1 } );
        builder.With( builder.Target.Constructors.Single() ).Override( nameof(Template), args: new { i = 2 } );
    }

    [Template]
    public void Template( [CompileTime] int i )
    {
        Console.WriteLine( $"This is the outer override {i}." );
        meta.Proceed();
    }
}

// <target>
[InnerOverride]
[OuterOverride]
public class TargetClass
{
    public TargetClass()
    {
        Console.WriteLine( $"This is the original constructor." );
    }
}