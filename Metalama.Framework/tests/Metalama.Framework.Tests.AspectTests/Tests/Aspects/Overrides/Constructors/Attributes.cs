using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Constructors.Attributes;

/*
 * Tests that overriding a constructor keeps all the existing attributes.
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
        Console.WriteLine( "This is the overridden constructor." );
    }
}

[AttributeUsage( AttributeTargets.Constructor )]
public class ConstructorOnlyAttribute : Attribute { }

[AttributeUsage( AttributeTargets.Constructor )]
public class ExplicitConstructorOnlyAttribute : Attribute { }

[AttributeUsage( AttributeTargets.Parameter )]
public class ParamOnlyAttribute : Attribute { }

// <target>
[Override]
internal class TargetClass
{
    [ConstructorOnly]
    [method: ExplicitConstructorOnly]
    public TargetClass( [ParamOnly] int x ) { }
}